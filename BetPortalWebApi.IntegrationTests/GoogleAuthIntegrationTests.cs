using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BetPortalWebApi.Services;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    [InlineData(true, null, "http://localhost:4200/sign-up-confirmation", false, true)]
    [InlineData(true, "/advisor", "http://localhost:4200/login", true, true)]
    [InlineData(false, null, null, false, true)]
    [InlineData(null, null, null, false, true)]
    [InlineData(true, null, null, false, true, false)]
    [InlineData(true, null, "http://localhost:4200/sign-up-confirmation", false, true, true, 150)]
    [InlineData(true, null, null, false, true, true, 151)]
    [InlineData(true, null, null, true, false, true, null, false)]
    public async Task Google_round_trip_requires_verified_email_and_redirects_after_sign_in(
        bool? emailVerified, string? returnUrl, string? destination, bool accountExists, bool register = false, bool emailSent = true,
        int? emailLength = null, bool linked = true)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        if (emailLength.HasValue)
        {
            email = email.PadLeft(emailLength.Value, 'a');
        }
        using var backchannel = new GoogleBackchannel(email, emailVerified);
        var emails = new RecordingEmailService(emailSent);
        using var client = _fixture.CreateGoogleClient(backchannel, emails);
        var cookies = new CookieContainer();
        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            if (accountExists)
            {
                await using var seed = _fixture.CreateDbContext();
                var existingUser = NewUser(email, isActive: true);
                if (linked && !register)
                {
                    existingUser.UserLoginProviders.Add(await NewProviderLinkAsync(seed, existingUser, email));
                }
                seed.BpUsers.Add(existingUser);
                await seed.SaveChangesAsync(cancellationToken);
            }

            var startUrl = register ? "/api/auth/google/register" : "/api/auth/google";
            if (returnUrl is not null)
            {
                startUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }
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
                Assert.Equal(
                    accountExists && !linked ? HttpStatusCode.Forbidden
                        : emailSent ? HttpStatusCode.Unauthorized : HttpStatusCode.ServiceUnavailable,
                    callback.StatusCode);
                if (!emailSent)
                {
                    var problem = await callback.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    Assert.Equal("Your account was registered, but the welcome email could not be sent", problem.GetProperty("title").GetString());
                }
                Assert.False(callback.Headers.TryGetValues("Set-Cookie", out var headers)
                    && headers.Any(header => header.StartsWith("AuthCookie=", StringComparison.Ordinal)
                        || header.StartsWith("SessionId=", StringComparison.Ordinal)));
            }
            else
            {
                Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
                Assert.NotNull(callback.Headers.Location);
                if (register)
                {
                    Assert.Equal(destination, callback.Headers.Location.GetLeftPart(UriPartial.Path));
                    if (accountExists)
                    {
                        Assert.Equal("account-exists", QueryHelpers.ParseQuery(callback.Headers.Location.Query)["googleError"].ToString());
                    }

                    Assert.DoesNotContain(callback.Headers.GetValues("Set-Cookie"), header =>
                        header.StartsWith("AuthCookie=", StringComparison.Ordinal)
                        || header.StartsWith("SessionId=", StringComparison.Ordinal));
                    ApplyCookies(client, cookies, callback, "/api/auth/check");
                    Assert.Empty(cookies.GetCookies(client.BaseAddress!)["GoogleExternal"]?.Value ?? string.Empty);
                    using var check = await client.GetAsync("/api/auth/check", cancellationToken);
                    var body = await check.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    Assert.False(body.GetProperty("isAuthenticated").GetBoolean());
                }
                else if (!accountExists)
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
            if (register && emailVerified == true && email.Length <= 150 && !accountExists)
            {
                Assert.NotNull(user);
                Assert.False(user.IsActive);
                Assert.Null(user.Password);
                Assert.Equal("Google", user.FirstName);
                Assert.Equal("User", user.Surname);
                var token = await dbContext.BpUserTokens.SingleAsync(token => token.UserId == user.Id, cancellationToken);
                Assert.Equal(64, token.UserInitToken.Length);
                Assert.Equal(32, Convert.FromHexString(token.UserInitToken).Length);
                Assert.Equal(user.CreatedDate, token.CreatedDate);
                var link = await dbContext.BpUserLoginProviders.Include(link => link.LoginProvider)
                    .SingleAsync(link => link.UserId == user.Id, cancellationToken);
                Assert.Equal("Google", link.LoginProvider.ProviderName);
                Assert.Equal(email, link.Email);
                Assert.False(await dbContext.BpUsers_BpRoles.AnyAsync(role => role.UserId == user.Id, cancellationToken));
                var message = Assert.Single(emails.Messages);
                Assert.Equal(email, message.Recipient);
                Assert.Equal("Welcome to Bet Portal", message.Subject);
                var activationUrl = $"http://localhost:4200/user-token?token={token.UserInitToken}";
                Assert.Contains($"href=\"{activationUrl}\"", message.Html);
                Assert.Contains(activationUrl, message.PlainText!);
                var service = CreateAccountService(dbContext, emails);
                Assert.Equal((false, false), await service.RegisterAsync(email.ToUpperInvariant(), null, null, cancellationToken));
                Assert.Single(emails.Messages);
                Assert.Equal(1, await dbContext.BpUsers.CountAsync(u => u.Email == email, cancellationToken));
                Assert.Equal(token.UserInitToken, (await dbContext.BpUserTokens.SingleAsync(t => t.UserId == user.Id, cancellationToken)).UserInitToken);
                Assert.Equal(1, await dbContext.BpUserLoginProviders.CountAsync(link => link.UserId == user.Id, cancellationToken));
            }
            else if (!accountExists)
            {
                Assert.Empty(emails.Messages);
                Assert.Null(user);
            }
            else
            {
                Assert.Empty(emails.Messages);
                Assert.NotNull(user);
                if (register)
                {
                    Assert.True(user.IsActive);
                    Assert.False(await dbContext.BpUserTokens.AnyAsync(t => t.UserId == user.Id, cancellationToken));
                    Assert.False(await dbContext.BpUserLoginProviders.AnyAsync(link => link.UserId == user.Id, cancellationToken));
                }
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
                var service = CreateAccountService(dbContext);
                var missing = await service.FindExistingAsync(email, TestContext.Current.CancellationToken);
                Assert.Null(missing.User);
                Assert.False(missing.AccountExists);
                Assert.False(await dbContext.BpUsers.AnyAsync(u => u.Email == email, TestContext.Current.CancellationToken));
                var created = NewUser(email, isActive: true);
                created.UserLoginProviders.Add(await NewProviderLinkAsync(dbContext, created, email));
                dbContext.BpUsers.Add(created);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
                createdId = created.Id;
            }

            await using (var dbContext = _fixture.CreateDbContext())
            {
                var service = CreateAccountService(dbContext);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Registration_commits_before_sending_encoded_welcome_email_and_preserves_account_on_delivery_failure(bool succeeds)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var cancellationToken = TestContext.Current.CancellationToken;
        string? savedToken = null;
        var emails = new RecordingEmailService(succeeds)
        {
            OnSend = async () =>
            {
                await using var persisted = _fixture.CreateDbContext();
                var user = await persisted.BpUsers.SingleAsync(u => u.Email == email, cancellationToken);
                Assert.False(user.IsActive);
                var link = await persisted.BpUserLoginProviders.Include(link => link.LoginProvider)
                    .SingleAsync(link => link.UserId == user.Id, cancellationToken);
                Assert.Equal("Google", link.LoginProvider.ProviderName);
                Assert.Equal(email, link.Email);
                savedToken = (await persisted.BpUserTokens.SingleAsync(t => t.UserId == user.Id, cancellationToken)).UserInitToken;
                var inactive = await CreateAccountService(persisted).FindExistingAsync(email, cancellationToken);
                Assert.True(inactive.AccountExists);
                Assert.Null(inactive.User);
            },
        };
        try
        {
            await using var dbContext = _fixture.CreateDbContext();
            var service = CreateAccountService(dbContext, emails);
            Assert.Equal((true, succeeds), await service.RegisterAsync(email, "<script>&\"Name", null, cancellationToken));
            Assert.NotNull(savedToken);
            var message = Assert.Single(emails.Messages);
            Assert.Equal(email, message.Recipient);
            Assert.Equal(cancellationToken, message.CancellationToken);
            Assert.Null(message.Bcc);
            Assert.Contains("Hello &lt;script&gt;&amp;&quot;Name", message.Html);
            Assert.DoesNotContain("<script>", message.Html);
            Assert.Contains($"https://portal.example.test/user-token?token={savedToken}", message.Html);
            Assert.Contains($"https://portal.example.test/user-token?token={savedToken}", message.PlainText!);
            Assert.Equal((false, false), await service.RegisterAsync(email, null, null, cancellationToken));
            Assert.Single(emails.Messages);
            await using var persisted = _fixture.CreateDbContext();
            Assert.Equal(savedToken, (await persisted.BpUserTokens.SingleAsync(t => t.User.Email == email, cancellationToken)).UserInitToken);
            if (succeeds)
            {
                var activationLink = new Uri(message.PlainText!.Split('\n').Single(line => line.StartsWith("https://", StringComparison.Ordinal)));
                var emailedToken = QueryHelpers.ParseQuery(activationLink.Query)["token"].ToString();
                await using var scope = _fixture.CreateTestScope();
                using var activation = await scope.Client.PostAsJsonAsync("/api/user-token", new { token = emailedToken }, cancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, activation.StatusCode);
                Assert.True(await persisted.BpUsers.Where(u => u.Email == email).Select(u => u.IsActive).SingleAsync(cancellationToken));
                Assert.False(await persisted.BpUserTokens.AnyAsync(t => t.User.Email == email, cancellationToken));
                var authenticated = await service.FindExistingAsync(email.ToUpperInvariant(), cancellationToken);
                Assert.True(authenticated.AccountExists);
                Assert.NotNull(authenticated.User);
            }
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    private static GoogleAccountService CreateAccountService(BpDbContext dbContext, IEmailService? emailService = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:FrontendOrigin"] = "https://portal.example.test",
        }).Build();
        return new GoogleAccountService(
            dbContext, emailService ?? new RecordingEmailService(), configuration, NullLogger<GoogleAccountService>.Instance);
    }

    [Fact]
    public async Task Account_service_rejects_existing_admin_without_google_link()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var service = CreateAccountService(dbContext);

        var (user, accountExists) = await service.FindExistingAsync("ADMIN@betportal.com", TestContext.Current.CancellationToken);

        Assert.True(accountExists);
        Assert.Null(user);
    }

    [Fact]
    public async Task Account_service_rejects_inactive_user_without_creating_duplicate()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        try
        {
            await using (var seed = _fixture.CreateDbContext())
            {
                var inactive = NewUser(email, isActive: false);
                inactive.UserLoginProviders.Add(await NewProviderLinkAsync(seed, inactive, email));
                seed.BpUsers.Add(inactive);
                await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using var dbContext = _fixture.CreateDbContext();
            var service = CreateAccountService(dbContext);

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
    public async Task Account_service_matches_provider_email_even_when_local_email_differs()
    {
        var localEmail = $"{Guid.NewGuid():N}@example.test";
        var googleEmail = $"{Guid.NewGuid():N}@Example.test";
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            await using var dbContext = _fixture.CreateDbContext();
            var created = NewUser(localEmail, isActive: true);
            created.UserLoginProviders.Add(await NewProviderLinkAsync(dbContext, created, googleEmail));
            dbContext.BpUsers.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);

            var emails = new RecordingEmailService();
            var service = CreateAccountService(dbContext, emails);
            var (user, accountExists) = await service.FindExistingAsync(googleEmail.ToUpperInvariant(), cancellationToken);

            Assert.True(accountExists);
            Assert.NotNull(user);
            Assert.Equal(created.Id, user.Id);
            Assert.Equal((false, false), await service.RegisterAsync(googleEmail.ToUpperInvariant(), null, null, cancellationToken));
            Assert.Empty(emails.Messages);
            Assert.False(await dbContext.BpUsers.AnyAsync(user => user.Email == googleEmail, cancellationToken));
            Assert.Equal(1, await dbContext.BpUserLoginProviders.CountAsync(link => link.UserId == created.Id, cancellationToken));
        }
        finally
        {
            await DeleteUsersAsync(localEmail);
        }
    }

    [Theory]
    [InlineData("Facebook", false)]
    [InlineData("Google", true)]
    public async Task Account_service_rejects_other_provider_or_mismatched_provider_email(string providerName, bool differentEmail)
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            await using var dbContext = _fixture.CreateDbContext();
            var created = NewUser(email, isActive: true);
            created.UserLoginProviders.Add(await NewProviderLinkAsync(
                dbContext, created, differentEmail ? $"{Guid.NewGuid():N}@example.test" : email, providerName));
            dbContext.BpUsers.Add(created);
            await dbContext.SaveChangesAsync(cancellationToken);

            var (user, accountExists) = await CreateAccountService(dbContext).FindExistingAsync(email, cancellationToken);

            Assert.True(accountExists);
            Assert.Null(user);
            Assert.Equal(1, await dbContext.BpUserLoginProviders.CountAsync(link => link.UserId == created.Id, cancellationToken));
        }
        finally
        {
            await DeleteUsersAsync(email);
        }
    }

    [Fact]
    public async Task Account_service_rejects_ambiguous_google_provider_email()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            await using var dbContext = _fixture.CreateDbContext();
            foreach (var isActive in new[] { true, false })
            {
                var created = NewUser(email, isActive);
                created.UserLoginProviders.Add(await NewProviderLinkAsync(dbContext, created, email));
                dbContext.BpUsers.Add(created);
            }
            await dbContext.SaveChangesAsync(cancellationToken);

            var (user, accountExists) = await CreateAccountService(dbContext).FindExistingAsync(email, cancellationToken);

            Assert.True(accountExists);
            Assert.Null(user);
            Assert.Equal(2, await dbContext.BpUserLoginProviders.CountAsync(link => link.Email == email, cancellationToken));
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

    private static async Task<BpUserLoginProvider> NewProviderLinkAsync(
        BpDbContext dbContext, BpUser user, string email, string providerName = "Google")
    {
        var provider = await dbContext.LoginProviders.SingleAsync(
            provider => provider.ProviderName == providerName, TestContext.Current.CancellationToken);
        return new BpUserLoginProvider
        {
            User = user,
            LoginProvider = provider,
            Email = email,
        };
    }

    private async Task DeleteUsersAsync(string email)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var users = await dbContext.BpUsers.Where(user => user.Email.ToLower() == email.ToLower()).ToListAsync();
        dbContext.BpUsers.RemoveRange(users);
        await dbContext.SaveChangesAsync();
    }
}
