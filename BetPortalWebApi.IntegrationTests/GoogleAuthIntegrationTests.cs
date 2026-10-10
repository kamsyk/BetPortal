using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BetPortalWebApi.Services;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

[Collection("API integration tests")]
public sealed class GoogleAuthIntegrationTests
{
    private readonly ApiFixture _fixture;

    public GoogleAuthIntegrationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Google_redirects_to_google_with_pkce_and_correlation_cookie()
    {
        await using var scope = _fixture.CreateTestScope();

        using var response = await scope.Client.GetAsync("/api/auth/google?returnUrl=%2Fadvisor", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        Assert.Equal("accounts.google.com", location.Host);
        Assert.Contains("client_id=test-client-id", location.Query);
        Assert.Contains("code_challenge=", location.Query);
        Assert.Contains(Uri.EscapeDataString("https://localhost/signin-google"), location.Query);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example")]
    public async Task Google_rejects_return_url_outside_frontend_origin(string returnUrl)
    {
        await using var scope = _fixture.CreateTestScope();

        using var response = await scope.Client.GetAsync(
            $"/api/auth/google?returnUrl={Uri.EscapeDataString(returnUrl)}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Google_callback_without_external_ticket_is_unauthorized_and_sets_no_session()
    {
        await using var scope = _fixture.CreateTestScope();

        using var response = await scope.Client.GetAsync("/api/auth/google/callback", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(
            response.Headers.TryGetValues("Set-Cookie", out var cookies)
            && cookies.Any(cookie => cookie.StartsWith("SessionId=", StringComparison.Ordinal) || cookie.StartsWith("AuthCookie=", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(true, null, "http://localhost:4200/", true)]
    [InlineData(true, "/advisor", "http://localhost:4200/advisor", true)]
    [InlineData(true, null, "http://localhost:4200/login", false)]
    [InlineData(true, "/advisor", "http://localhost:4200/login", false)]
    [InlineData(false, null, null, false)]
    [InlineData(null, null, null, false)]
    public async Task Google_round_trip_requires_verified_email_and_redirects_after_sign_in(
        bool? emailVerified, string? returnUrl, string? destination, bool accountExists)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        using var backchannel = new GoogleBackchannel(email, emailVerified);
        using var client = _fixture.CreateGoogleClient(backchannel);
        var cookies = new CookieContainer();
        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            if (accountExists)
            {
                await using var seed = _fixture.CreateDbContext();
                seed.BpUsers.Add(NewUser(email, isActive: true));
                await seed.SaveChangesAsync(cancellationToken);
            }

            var startUrl = returnUrl is null
                ? "/api/auth/google"
                : $"/api/auth/google?returnUrl={Uri.EscapeDataString(returnUrl)}";
            using var challenge = await client.GetAsync(startUrl, cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
            Assert.NotNull(challenge.Headers.Location);
            var state = QueryHelpers.ParseQuery(challenge.Headers.Location.Query)["state"].ToString();
            Assert.NotEmpty(state);
            ApplyCookies(client, cookies, challenge, "/signin-google");

            using var googleResponse = await client.GetAsync(
                $"/signin-google?code=test-code&state={Uri.EscapeDataString(state)}", cancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, googleResponse.StatusCode);
            Assert.NotNull(googleResponse.Headers.Location);
            Assert.Equal("/api/Auth/google/callback", googleResponse.Headers.Location.OriginalString);
            ApplyCookies(client, cookies, googleResponse, "/api/Auth/google/callback");

            using var callback = await client.GetAsync(googleResponse.Headers.Location, cancellationToken);
            if (destination is null)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);
                Assert.False(callback.Headers.TryGetValues("Set-Cookie", out var headers)
                    && headers.Any(header => header.StartsWith("AuthCookie=", StringComparison.Ordinal)
                        || header.StartsWith("SessionId=", StringComparison.Ordinal)));
            }
            else
            {
                Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
                Assert.NotNull(callback.Headers.Location);
                if (!accountExists)
                {
                    Assert.Equal(destination, callback.Headers.Location.GetLeftPart(UriPartial.Path));
                    var query = QueryHelpers.ParseQuery(callback.Headers.Location.Query);
                    Assert.Equal("account-required", query["googleError"].ToString());
                    Assert.Equal(returnUrl ?? "/", query["returnUrl"].ToString());
                    var rejectedHeaders = callback.Headers.GetValues("Set-Cookie").ToList();
                    Assert.DoesNotContain(rejectedHeaders, header =>
                        header.StartsWith("AuthCookie=", StringComparison.Ordinal)
                        || header.StartsWith("SessionId=", StringComparison.Ordinal));
                    ApplyCookies(client, cookies, callback, "/api/auth/check");
                    Assert.Empty(cookies.GetCookies(client.BaseAddress!)["GoogleExternal"]?.Value ?? string.Empty);
                    using var check = await client.GetAsync("/api/auth/check", cancellationToken);
                    var body = await check.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    Assert.False(body.GetProperty("isAuthenticated").GetBoolean());
                }
                else
                {
                    Assert.Equal(destination, callback.Headers.Location.AbsoluteUri);
                    var headers = callback.Headers.GetValues("Set-Cookie").ToList();
                    Assert.Contains(headers, header => header.StartsWith("AuthCookie=", StringComparison.Ordinal));
                    Assert.Contains(headers, header => header.StartsWith("SessionId=", StringComparison.Ordinal));
                    ApplyCookies(client, cookies, callback, "/api/auth/check");
                    Assert.Empty(cookies.GetCookies(client.BaseAddress!)["GoogleExternal"]?.Value ?? string.Empty);

                    foreach (var cookieName in new[] { "AuthCookie", "SessionId" })
                    {
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
            }

            await using var dbContext = _fixture.CreateDbContext();
            var user = await dbContext.BpUsers.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
            if (!accountExists)
            {
                Assert.Null(user);
            }
            else
            {
                Assert.NotNull(user);
                Assert.False(await dbContext.BpUsers_BpRoles.AnyAsync(role => role.UserId == user.Id, cancellationToken));
            }

            Assert.Equal(1, backchannel.TokenRequests);
            Assert.Equal(1, backchannel.UserInfoRequests);
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    private static void ApplyCookies(HttpClient client, CookieContainer cookies, HttpResponseMessage response, string nextPath)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
        {
            foreach (var header in headers)
            {
                cookies.SetCookies(client.BaseAddress!, header);
            }
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        var cookieHeader = cookies.GetCookieHeader(new Uri(client.BaseAddress!, nextPath));
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        }
    }

    private sealed class GoogleBackchannel(string email, bool? emailVerified) : HttpMessageHandler
    {
        public int TokenRequests { get; private set; }

        public int UserInfoRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsoluteUri == "https://oauth2.googleapis.com/token")
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.NotNull(request.Content);
                var form = QueryHelpers.ParseQuery(await request.Content.ReadAsStringAsync(cancellationToken));
                Assert.Equal("https://localhost/signin-google", form["redirect_uri"].ToString());
                Assert.Equal("test-code", form["code"].ToString());
                Assert.NotEmpty(form["code_verifier"].ToString());
                TokenRequests++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { access_token = "test-token", token_type = "Bearer", expires_in = 3600 }),
                };
            }

            Assert.Equal("https://www.googleapis.com/oauth2/v3/userinfo", request.RequestUri?.AbsoluteUri);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            UserInfoRequests++;
            var profile = new Dictionary<string, object>
            {
                ["sub"] = "test-google-user",
                ["email"] = email,
                ["given_name"] = "Google",
                ["family_name"] = "User",
                ["name"] = "Google User",
            };
            if (emailVerified.HasValue)
            {
                profile["email_verified"] = emailVerified.Value;
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(profile),
            };
        }
    }

    [Fact]
    public async Task Account_service_does_not_create_unknown_user_and_matches_existing_case_insensitively()
    {
        var email = $"{Guid.NewGuid():N}@Example.test";
        try
        {
            int createdId;
            await using (var dbContext = _fixture.CreateDbContext())
            {
                var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);
                var missing = await service.FindExistingAsync(email, TestContext.Current.CancellationToken);
                Assert.Null(missing.User);
                Assert.False(missing.AccountExists);
                Assert.False(await dbContext.BpUsers.AnyAsync(u => u.Email == email, TestContext.Current.CancellationToken));
                var created = NewUser(email, isActive: true);
                dbContext.BpUsers.Add(created);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                createdId = created.Id;
            }

            await using (var dbContext = _fixture.CreateDbContext())
            {
                var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);
                var (existing, accountExists) = await service.FindExistingAsync(email.ToUpperInvariant(), TestContext.Current.CancellationToken);
                Assert.True(accountExists);
                Assert.NotNull(existing);
                Assert.Equal(createdId, existing.Id);
                Assert.False(await dbContext.BpUsers_BpRoles.AnyAsync(userRole => userRole.UserId == createdId, TestContext.Current.CancellationToken));
            }
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    [Fact]
    public async Task Account_service_links_existing_admin_by_email()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);

        var (user, accountExists) = await service.FindExistingAsync("ADMIN@betportal.com", TestContext.Current.CancellationToken);

        Assert.True(accountExists);
        Assert.NotNull(user);
        Assert.Equal("admin", user.UserName);
    }

    [Fact]
    public async Task Account_service_rejects_inactive_user_without_creating_duplicate()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        try
        {
            await using (var seed = _fixture.CreateDbContext())
            {
                seed.BpUsers.Add(NewUser(email, isActive: false));
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var dbContext = _fixture.CreateDbContext();
            var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);

            var (user, accountExists) = await service.FindExistingAsync(email, TestContext.Current.CancellationToken);

            Assert.True(accountExists);
            Assert.Null(user);
            Assert.Equal(1, await dbContext.BpUsers.CountAsync(u => u.Email == email, TestContext.Current.CancellationToken));
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    [Fact]
    public async Task User_without_admin_role_is_authenticated_but_denied_admin_endpoints()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var user = NewUser(email, isActive: true);
        user.Password = PasswordHasher.HashPassword("pass-123");
        try
        {
            await using (var seed = _fixture.CreateDbContext())
            {
                seed.BpUsers.Add(user);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var scope = _fixture.CreateTestScope();
            using var login = await scope.Client.PostAsJsonAsync("/api/auth/login", new { Username = user.UserName, Password = "pass-123" }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var cookies = login.Headers.GetValues("Set-Cookie").Select(cookie => cookie.Split(';', 2)[0]).ToList();

            foreach (var cookieName in new[] { "SessionId", "AuthCookie" })
            {
                scope.Client.DefaultRequestHeaders.Remove("Cookie");
                scope.Client.DefaultRequestHeaders.Add("Cookie", cookies.Single(cookie => cookie.StartsWith(cookieName + "=", StringComparison.Ordinal)));

                using var read = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);
                using var write = await scope.Client.PostAsJsonAsync("/api/sport/add", new { Name = $"{scope.NamePrefix}{cookieName}", Active = true }, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, read.StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
            }
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    private static BpUser NewUser(string email, bool isActive) => new()
    {
        UserName = $"it-{Guid.NewGuid():N}",
        Email = email,
        IsActive = isActive,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow,
        CreatedByUserId = 1,
        ModifiedByUserId = 1,
    };

    private async Task DeleteUsersAsync(string email)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var users = await dbContext.BpUsers.Where(user => user.Email.ToLower() == email.ToLower()).ToListAsync();
        dbContext.BpUsers.RemoveRange(users);
        await dbContext.SaveChangesAsync();
    }
}
