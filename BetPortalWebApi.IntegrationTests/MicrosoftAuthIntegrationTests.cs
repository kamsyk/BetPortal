using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BpDbLayer.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

[Collection("API integration tests")]
public sealed class MicrosoftAuthIntegrationTests(ApiFixture fixture)
{
    [Theory]
    [InlineData("/api/auth/microsoft")]
    [InlineData("/api/auth/microsoft/register")]
    public async Task Challenge_supports_personal_and_organizational_accounts_with_pkce(string path)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(path + "?returnUrl=%2Fadvisor", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("login.microsoftonline.com", location.Host);
        Assert.Equal("/common/oauth2/v2.0/authorize", location.AbsolutePath);
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("test-microsoft-client-id", query["client_id"]);
        Assert.Equal("https://localhost/signin-microsoft", query["redirect_uri"]);
        Assert.Contains("https://graph.microsoft.com/user.read", query["scope"].ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("S256", query["code_challenge_method"]);
        Assert.NotEmpty(query["code_challenge"].ToString());
        Assert.NotEmpty(query["state"].ToString());
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example")]
    public async Task Challenge_rejects_external_return_urls(string returnUrl)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(
            "/api/auth/microsoft?returnUrl=" + Uri.EscapeDataString(returnUrl), TestContext.Current.CancellationToken);
        ExternalAuthAssert.LoginErrorRedirect(response, "microsoftError", "sign-in-failed", "/");
    }

    [Fact]
    public async Task Challenge_requires_frontend_configuration()
    {
        using var backchannel = new MicrosoftBackchannel("unused@example.test");
        using var client = fixture.CreateMicrosoftClient(backchannel, frontendOrigin: string.Empty);
        using var response = await client.GetAsync("/api/auth/microsoft", TestContext.Current.CancellationToken);
        await ExternalAuthAssert.SanitizedProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("/api/auth/microsoft/callback")]
    [InlineData("/signin-microsoft?error=access_denied")]
    [InlineData("/signin-microsoft?code=fake-code&state=invalid")]
    public async Task Invalid_ticket_or_state_never_issues_application_cookies(string path)
    {
        await using var scope = fixture.CreateTestScope();
        using var response = await scope.Client.GetAsync(path, TestContext.Current.CancellationToken);
        ExternalAuthAssert.LoginErrorRedirect(response, "microsoftError", "sign-in-failed", "/");
        AssertNoApplicationCookies(response);
    }

    [Theory]
    [InlineData(false, null, true, 50, true, HttpStatusCode.Redirect, "account-required")]
    [InlineData(false, "Microsoft", true, 50, true, HttpStatusCode.Redirect, null)]
    [InlineData(false, "Microsoft", false, 50, true, HttpStatusCode.Redirect, "account-inactive")]
    [InlineData(false, "Google", true, 50, true, HttpStatusCode.Redirect, "account-not-linked")]
    [InlineData(false, "Facebook", true, 50, true, HttpStatusCode.Redirect, "account-not-linked")]
    [InlineData(false, "", true, 50, true, HttpStatusCode.Redirect, "account-not-linked")]
    [InlineData(true, null, true, 50, true, HttpStatusCode.Redirect, null)]
    [InlineData(true, "Microsoft", true, 50, true, HttpStatusCode.Redirect, "account-exists")]
    [InlineData(true, "", true, 50, true, HttpStatusCode.Redirect, "account-exists")]
    [InlineData(true, "Google", true, 50, true, HttpStatusCode.Redirect, "account-exists")]
    [InlineData(true, null, true, 0, true, HttpStatusCode.Redirect, "email-unavailable")]
    [InlineData(false, null, true, 0, true, HttpStatusCode.Redirect, "email-unavailable")]
    [InlineData(true, null, true, 150, true, HttpStatusCode.Redirect, null)]
    [InlineData(true, null, true, 151, true, HttpStatusCode.Redirect, "email-unavailable")]
    [InlineData(true, null, true, 50, false, HttpStatusCode.Redirect, "activation-email-failed")]
    public async Task Round_trip_enforces_provider_links_activation_and_registration_intent(
        bool register, string? existingProvider, bool active, int emailLength, bool emailSent,
        HttpStatusCode expectedStatus, string? expectedError)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var email = $"{Guid.NewGuid():N}@example.test".PadLeft(Math.Max(50, emailLength), 'a');
        using var backchannel = new MicrosoftBackchannel(emailLength == 0 ? null : email.ToUpperInvariant());
        var emails = new RecordingEmailService(emailSent);
        using var client = fixture.CreateMicrosoftClient(backchannel, emails);
        var cookies = new CookieContainer();
        try
        {
            if (existingProvider is not null)
            {
                await using var seed = fixture.CreateDbContext();
                var user = new BpUser
                {
                    UserName = $"it-{Guid.NewGuid():N}",
                    Email = email,
                    IsActive = active,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    CreatedByUserId = 1,
                    ModifiedByUserId = 1,
                };
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

            var start = register ? "/api/auth/microsoft/register" : "/api/auth/microsoft";
            using var challenge = await client.GetAsync(start + "?returnUrl=%2Fadvisor", cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
            var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
            ApplyCookies(client, cookies, challenge, "/signin-microsoft");
            using var external = await client.GetAsync(
                "/signin-microsoft?code=test-code&state=" + Uri.EscapeDataString(state), cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, external.StatusCode);
            Assert.Equal("/api/Auth/microsoft/callback", external.Headers.Location!.OriginalString);
            ApplyCookies(client, cookies, external, "/api/auth/microsoft/callback");
            using var callback = await client.GetAsync(external.Headers.Location, cancellationToken);
            Assert.Equal(expectedStatus, callback.StatusCode);
            if (expectedError is not null)
            {
                ExternalAuthAssert.LoginErrorRedirect(callback, "microsoftError", expectedError, "/advisor");
            }

            var loggedIn = !register && existingProvider == "Microsoft" && active && emailLength > 0;
            ApplyCookies(client, cookies, callback, "/api/auth/check");
            Assert.Empty(cookies.GetCookies(client.BaseAddress!)["MicrosoftExternal"]?.Value ?? string.Empty);
            if (loggedIn)
            {
                Assert.Equal("http://localhost:4200/advisor", callback.Headers.Location!.AbsoluteUri);
                foreach (var name in new[] { "AuthCookie", "SessionId" })
                {
                    var cookie = cookies.GetCookies(client.BaseAddress!)[name];
                    Assert.NotNull(cookie);
                    client.DefaultRequestHeaders.Remove("Cookie");
                    client.DefaultRequestHeaders.Add("Cookie", $"{name}={cookie.Value}");
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
            var saved = await persisted.BpUsers.SingleOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
            var created = register && existingProvider is null && emailLength is > 0 and <= 150;
            if (created)
            {
                Assert.NotNull(saved);
                Assert.False(saved.IsActive);
                Assert.Null(saved.Password);
                Assert.Equal("Microsoft", saved.FirstName);
                Assert.Equal("User", saved.Surname);
                var link = await persisted.BpUserLoginProviders.Include(l => l.LoginProvider)
                    .SingleAsync(l => l.UserId == saved.Id, cancellationToken);
                Assert.Equal("Microsoft", link.LoginProvider.ProviderName);
                Assert.Equal(email.ToUpperInvariant(), link.Email);
                var token = await persisted.BpUserTokens.SingleAsync(t => t.UserId == saved.Id, cancellationToken);
                Assert.Equal(64, token.UserInitToken.Length);
                Assert.Contains(token.UserInitToken, Assert.Single(emails.Messages).PlainText!);
                if (emailSent)
                {
                    Assert.Equal("http://localhost:4200/sign-up-confirmation", callback.Headers.Location!.AbsoluteUri);
                    using var activation = await client.PostAsJsonAsync("/api/user-token", new { token = token.UserInitToken }, cancellationToken);
                    Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
                    await persisted.Entry(saved).ReloadAsync(cancellationToken);
                    Assert.True(saved.IsActive);
                    Assert.False(await persisted.BpUserTokens.AnyAsync(t => t.UserId == saved.Id, cancellationToken));
                }
            }
            else
            {
                Assert.Empty(emails.Messages);
                if (existingProvider is null)
                {
                    Assert.Null(saved);
                }
                else
                {
                    Assert.NotNull(saved);
                    Assert.False(await persisted.BpUserTokens.AnyAsync(t => t.UserId == saved.Id, cancellationToken));
                    Assert.Equal(existingProvider.Length > 0 ? 1 : 0,
                        await persisted.BpUserLoginProviders.CountAsync(l => l.UserId == saved.Id, cancellationToken));
                }
            }

            Assert.Equal(1, backchannel.TokenRequests);
            Assert.Equal(1, backchannel.ProfileRequests);
            using var replay = await client.GetAsync("/api/auth/microsoft/callback", cancellationToken);
            ExternalAuthAssert.LoginErrorRedirect(replay, "microsoftError", "sign-in-failed", "/");
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
        var headerValue = cookies.GetCookieHeader(new Uri(client.BaseAddress!, path));
        if (headerValue.Length > 0)
        {
            client.DefaultRequestHeaders.Add("Cookie", headerValue);
        }
    }

    private sealed class MicrosoftBackchannel(string? email) : HttpMessageHandler
    {
        public int TokenRequests { get; private set; }

        public int ProfileRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host == "login.microsoftonline.com")
            {
                Assert.Equal("/common/oauth2/v2.0/token", request.RequestUri.AbsolutePath);
                Assert.Equal(HttpMethod.Post, request.Method);
                var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
                Assert.Equal("test-microsoft-client-id", form["client_id"]);
                Assert.Equal("test-microsoft-client-secret", form["client_secret"]);
                Assert.Equal("test-code", form["code"]);
                Assert.Equal("https://localhost/signin-microsoft", form["redirect_uri"]);
                Assert.NotEmpty(form["code_verifier"].ToString());
                TokenRequests++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { access_token = "test-microsoft-token", token_type = "Bearer", expires_in = 3600 }),
                };
            }

            Assert.Equal("graph.microsoft.com", request.RequestUri.Host);
            Assert.Equal("/v1.0/me", request.RequestUri.AbsolutePath);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-microsoft-token", request.Headers.Authorization?.Parameter);
            ProfileRequests++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = "test-microsoft-user",
                    mail = email,
                    userPrincipalName = "not-a-mailbox@example.test",
                    displayName = "Microsoft User",
                    givenName = "Microsoft",
                    surname = "User",
                }),
            };
        }
    }
}
