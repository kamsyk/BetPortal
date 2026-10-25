using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BetPortalWebApi.Services;
using BpDbLayer.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

[Collection("API integration tests")]
public sealed class FacebookAuthIntegrationTests(ApiFixture fixture)
{
    [Theory]
    [InlineData("/api/auth/facebook")]
    [InlineData("/api/auth/facebook/register")]
    public async Task Challenge_requests_email_and_uses_correlation_cookie(string path)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(path + "?returnUrl=%2Fadvisor", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.Equal("www.facebook.com", location.Host);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("test-facebook-app-id", query["client_id"]);
        Assert.Equal("https://localhost/signin-facebook", query["redirect_uri"]);
        Assert.Contains("email", query["scope"].ToString());
        Assert.NotEmpty(query["state"].ToString());
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example")]
    public async Task Challenge_rejects_external_return_url(string returnUrl)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(
            $"/api/auth/facebook?returnUrl={Uri.EscapeDataString(returnUrl)}", TestContext.Current.CancellationToken);
        ExternalAuthAssert.LoginErrorRedirect(response, "facebookError", "sign-in-failed", "/");
    }

    [Fact]
    public async Task Challenge_returns_unavailable_without_frontend_origin()
    {
        using var backchannel = new FacebookBackchannel("unused@example.test");
        using var client = fixture.CreateFacebookClient(backchannel, frontendOrigin: string.Empty);
        using var response = await client.GetAsync("/api/auth/facebook", TestContext.Current.CancellationToken);
        await ExternalAuthAssert.SanitizedProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("/api/auth/facebook/callback")]
    [InlineData("/signin-facebook?error=access_denied")]
    [InlineData("/signin-facebook?code=fake-code&state=invalid")]
    public async Task Invalid_ticket_or_oauth_failure_creates_no_application_session(string path)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(path, TestContext.Current.CancellationToken);
        ExternalAuthAssert.LoginErrorRedirect(response, "facebookError", "sign-in-failed", "/");
        AssertNoApplicationCookies(response);
    }

    [Theory]
    [InlineData(false, null, true, true, true, HttpStatusCode.Redirect, "account-required")]
    [InlineData(false, "Facebook", true, true, true, HttpStatusCode.Redirect, null)]
    [InlineData(false, "Facebook", false, true, true, HttpStatusCode.Redirect, "account-inactive")]
    [InlineData(false, "Google", true, true, true, HttpStatusCode.Redirect, "account-not-linked")]
    [InlineData(false, "", true, true, true, HttpStatusCode.Redirect, "account-not-linked")]
    [InlineData(true, null, true, true, true, HttpStatusCode.Redirect, null)]
    [InlineData(true, "Facebook", true, true, true, HttpStatusCode.Redirect, "account-exists")]
    [InlineData(true, "Google", true, true, true, HttpStatusCode.Redirect, "account-exists")]
    [InlineData(true, null, true, false, true, HttpStatusCode.Redirect, "email-unavailable")]
    [InlineData(false, null, true, false, true, HttpStatusCode.Redirect, "email-unavailable")]
    [InlineData(true, null, true, true, false, HttpStatusCode.Redirect, "activation-email-failed")]
    [InlineData(true, null, true, true, true, HttpStatusCode.Redirect, null, 150)]
    [InlineData(true, null, true, true, true, HttpStatusCode.Redirect, "email-unavailable", 151)]
    public async Task Round_trip_checks_provider_link_and_registration_intent(
        bool register, string? existingProvider, bool active, bool hasEmail, bool emailSent,
        HttpStatusCode expectedStatus, string? expectedError, int? emailLength = null)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        if (emailLength.HasValue)
        {
            email = email.PadLeft(emailLength.Value, 'a');
        }
        var cancellationToken = TestContext.Current.CancellationToken;
        using var backchannel = new FacebookBackchannel(hasEmail ? email.ToUpperInvariant() : null);
        var emails = new RecordingEmailService(emailSent);
        using var client = fixture.CreateFacebookClient(backchannel, emails);
        var cookies = new CookieContainer();
        try
        {
            if (existingProvider is not null)
            {
                await using var seed = fixture.CreateDbContext();
                var user = NewUser(email, active);
                if (existingProvider.Length > 0)
                {
                    user.UserLoginProviders.Add(new BpUserLoginProvider
                    {
                        User = user,
                        Email = email,
                        LoginProvider = await seed.LoginProviders.SingleAsync(p => p.ProviderName == existingProvider, cancellationToken),
                    });
                }
                seed.BpUsers.Add(user);
                await seed.SaveChangesAsync(cancellationToken);
            }

            var start = register ? "/api/auth/facebook/register" : "/api/auth/facebook";
            using var challenge = await client.GetAsync(start + "?returnUrl=%2Fadvisor", cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
            var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
            ApplyCookies(client, cookies, challenge, "/signin-facebook");
            using var external = await client.GetAsync(
                $"/signin-facebook?code=test-code&state={Uri.EscapeDataString(state)}", cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, external.StatusCode);
            Assert.Equal("/api/Auth/facebook/callback", external.Headers.Location!.OriginalString);
            ApplyCookies(client, cookies, external, "/api/auth/facebook/callback");

            using var callback = await client.GetAsync(external.Headers.Location, cancellationToken);
            Assert.Equal(expectedStatus, callback.StatusCode);
            if (expectedError is not null)
            {
                ExternalAuthAssert.LoginErrorRedirect(callback, "facebookError", expectedError, "/advisor");
                if (expectedError == "account-not-linked")
                {
                    await ExternalAuthAssert.NotLinkedDetailsAsync(
                        client, callback, email.ToUpperInvariant(), existingProvider!.Length > 0 ? [existingProvider] : []);
                }
            }

            var loggedIn = !register && existingProvider == "Facebook" && active && hasEmail;
            ApplyCookies(client, cookies, callback, "/api/auth/check");
            Assert.Empty(cookies.GetCookies(client.BaseAddress!)["FacebookExternal"]?.Value ?? string.Empty);
            if (loggedIn)
            {
                Assert.Equal("http://localhost:4200/advisor", callback.Headers.Location!.AbsoluteUri);
                foreach (var cookieName in new[] { "AuthCookie", "SessionId" })
                {
                    Assert.Contains(callback.Headers.GetValues("Set-Cookie"),
                        cookie => cookie.StartsWith(cookieName + "=", StringComparison.Ordinal));
                    client.DefaultRequestHeaders.Remove("Cookie");
                    var cookie = cookies.GetCookies(client.BaseAddress!)[cookieName];
                    Assert.NotNull(cookie);
                    client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value}");
                    using var check = await client.GetAsync("/api/auth/check", cancellationToken);
                    Assert.Equal(HttpStatusCode.OK, check.StatusCode);
                    var body = await check.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    Assert.True(body.GetProperty("isAuthenticated").GetBoolean());
                }
            }
            else
            {
                AssertNoApplicationCookies(callback);
                using var check = await client.GetAsync("/api/auth/check", cancellationToken);
                var body = await check.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                Assert.False(body.GetProperty("isAuthenticated").GetBoolean());
            }

            await using var persisted = fixture.CreateDbContext();
            var savedUser = await persisted.BpUsers.SingleOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
            var created = register && existingProvider is null && hasEmail && email.Length <= 150;
            if (created)
            {
                Assert.NotNull(savedUser);
                Assert.False(savedUser.IsActive);
                Assert.Null(savedUser.Password);
                Assert.Equal("Facebook", savedUser.FirstName);
                Assert.Equal("User", savedUser.Surname);
                var link = await persisted.BpUserLoginProviders.Include(link => link.LoginProvider)
                    .SingleAsync(link => link.UserId == savedUser.Id, cancellationToken);
                Assert.Equal("Facebook", link.LoginProvider.ProviderName);
                Assert.Equal(email.ToUpperInvariant(), link.Email);
                var token = await persisted.BpUserTokens.SingleAsync(t => t.UserId == savedUser.Id, cancellationToken);
                Assert.Equal(64, token.UserInitToken.Length);
                Assert.Contains(token.UserInitToken, Assert.Single(emails.Messages).PlainText!);
                if (emailSent)
                {
                    Assert.Equal("http://localhost:4200/sign-up-confirmation", callback.Headers.Location!.AbsoluteUri);
                    using var activation = await client.PostAsJsonAsync("/api/user-token", new { token = token.UserInitToken }, cancellationToken);
                    Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
                    var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Facebook:FrontendOrigin"] = "http://localhost:4200",
                    }).Build();
                    var service = new FacebookAccountService(persisted, emails, configuration, NullLogger<FacebookAccountService>.Instance);
                    Assert.NotNull((await service.FindExistingAsync(email, cancellationToken)).User);
                }
            }
            else
            {
                Assert.Empty(emails.Messages);
                if (existingProvider is null)
                {
                    Assert.Null(savedUser);
                }
                else
                {
                    Assert.NotNull(savedUser);
                    Assert.False(await persisted.BpUserTokens.AnyAsync(t => t.UserId == savedUser.Id, cancellationToken));
                    Assert.Equal(existingProvider.Length > 0 ? 1 : 0,
                        await persisted.BpUserLoginProviders.CountAsync(link => link.UserId == savedUser.Id, cancellationToken));
                }
            }
            Assert.Equal(1, backchannel.TokenRequests);
            Assert.Equal(1, backchannel.ProfileRequests);
            using var replay = await client.GetAsync("/api/auth/facebook/callback", cancellationToken);
            ExternalAuthAssert.LoginErrorRedirect(replay, "facebookError", "sign-in-failed", "/");
            AssertNoApplicationCookies(replay);
        }
        finally
        {
            await using var cleanup = fixture.CreateDbContext();
            cleanup.BpUsers.RemoveRange(await cleanup.BpUsers.Where(u => u.Email.ToLower() == email).ToListAsync(cancellationToken));
            await cleanup.SaveChangesAsync(cancellationToken);
        }
    }

    private static void AssertNoApplicationCookies(HttpResponseMessage response)
    {
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.StartsWith("AuthCookie=", StringComparison.Ordinal)
                || cookie.StartsWith("SessionId=", StringComparison.Ordinal)));
    }

    private static void ApplyCookies(HttpClient client, CookieContainer cookies, HttpResponseMessage response, string path)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            foreach (var header in headers)
            {
                cookies.SetCookies(client.BaseAddress!, header);
            }
        }
        client.DefaultRequestHeaders.Remove("Cookie");
        var cookieHeader = cookies.GetCookieHeader(new Uri(client.BaseAddress!, path));
        if (cookieHeader.Length > 0)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
    }

    private static BpUser NewUser(string email, bool active) => new()
    {
        UserName = $"it-{Guid.NewGuid():N}",
        Email = email,
        IsActive = active,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow,
        CreatedByUserId = 1,
        ModifiedByUserId = 1,
    };

    private sealed class FacebookBackchannel(string? email) : HttpMessageHandler
    {
        public int TokenRequests { get; private set; }

        public int ProfileRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("graph.facebook.com", request.RequestUri?.Host);
            if (request.RequestUri!.AbsolutePath.EndsWith("/oauth/access_token", StringComparison.Ordinal))
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
                Assert.Equal("test-facebook-app-id", form["client_id"]);
                Assert.Equal("test-facebook-app-secret", form["client_secret"]);
                Assert.Equal("https://localhost/signin-facebook", form["redirect_uri"]);
                Assert.Equal("test-code", form["code"]);
                TokenRequests++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { access_token = "test-facebook-token", token_type = "Bearer", expires_in = 3600 }),
                };
            }
            Assert.EndsWith("/me", request.RequestUri.AbsolutePath);
            Assert.Equal(HttpMethod.Get, request.Method);
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Contains("email", query["fields"].ToString());
            Assert.Contains("first_name", query["fields"].ToString());
            Assert.NotEmpty(query["appsecret_proof"].ToString());
            ProfileRequests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = "test-facebook-user",
                    email,
                    name = "Facebook User",
                    first_name = "Facebook",
                    last_name = "User",
                }),
            };
        }
    }
}
