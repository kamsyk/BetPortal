using System.Net;
using System.Net.Http.Json;
using BetPortalWebApi.Services;
using BpDbLayer.Models;
using BpDbLayer.Utils;
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

    [Fact]
    public async Task Account_service_creates_new_non_admin_user_and_reuses_it_case_insensitively()
    {
        var email = $"{Guid.NewGuid():N}@Example.test";
        try
        {
            int createdId;
            await using (var dbContext = _fixture.CreateDbContext())
            {
                var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);
                var created = await service.FindOrCreateAsync(email, "New", "User", TestContext.Current.CancellationToken);
                Assert.NotNull(created);
                Assert.True(created.IsActive);
                Assert.Null(created.Password);
                Assert.Equal("New", created.FirstName);
                createdId = created.Id;
            }

            await using (var dbContext = _fixture.CreateDbContext())
            {
                var service = new GoogleAccountService(dbContext, NullLogger<GoogleAccountService>.Instance);
                var existing = await service.FindOrCreateAsync(email.ToUpperInvariant(), "Other", "Name", TestContext.Current.CancellationToken);
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

        var user = await service.FindOrCreateAsync("ADMIN@betportal.com", null, null, TestContext.Current.CancellationToken);

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

            var user = await service.FindOrCreateAsync(email, null, null, TestContext.Current.CancellationToken);

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
