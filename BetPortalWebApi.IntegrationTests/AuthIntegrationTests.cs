using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using BpDbLayer.ModelConfigurations;
using BpDbLayer.Models;
using BpDbLayer.Utils;
using Microsoft.EntityFrameworkCore;

[Collection("API integration tests")]
public sealed class AuthIntegrationTests
{
    private readonly ApiFixture _fixture;

    public AuthIntegrationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_sets_only_hashed_database_session_cookie_and_check_reports_authenticated()
    {
        await using var scope = _fixture.CreateTestScope();
        var login = await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        Assert.True(login.Success);
        Assert.Equal("admin", login.User.Username);
        Assert.Equal(new[] { "SessionId" }, scope.Cookies.Keys);

        var sessionId = scope.Cookies["SessionId"];
        Assert.Equal(43, sessionId.Length);
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var storedSession = await dbContext.BpUserSessions.SingleAsync(
                session => session.TokenHash == expectedHash,
                TestContext.Current.CancellationToken);
            Assert.NotEqual(sessionId, storedSession.TokenHash);
            Assert.True(storedSession.ExpiresDate > DateTime.UtcNow);
        }

        using var response = await scope.Client.GetAsync("/api/auth/check", TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<AuthCheckResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.IsAuthenticated);
        Assert.True(result.SessionValid);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task Login_rejects_invalid_credentials()
    {
        await using var scope = _fixture.CreateTestScope();

        using var response = await scope.Client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = "admin",
            Password = "not-the-password",
        }, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("Invalid credentials", result.Message);
    }

    [Fact]
    public async Task Session_is_rejected_when_user_becomes_inactive()
    {
        var userName = $"it-{Guid.NewGuid():N}";
        var email = $"{userName}@example.test";
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = _fixture.CreateTestScope();

        try
        {
            await using (var seedContext = _fixture.CreateDbContext())
            {
                seedContext.BpUsers.Add(new BpUser
                {
                    UserName = userName,
                    Email = email,
                    Password = PasswordHasher.HashPassword("test-password"),
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = 1,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = 1,
                });
                await seedContext.SaveChangesAsync(cancellationToken);
            }

            using var loginResponse = await scope.Client.PostAsJsonAsync("/api/auth/login", new
            {
                Username = userName,
                Password = "test-password",
            }, cancellationToken);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            var sessionCookie = loginResponse.Headers.GetValues("Set-Cookie")
                .Single(cookie => cookie.StartsWith("SessionId=", StringComparison.Ordinal))
                .Split(';', 2, StringSplitOptions.TrimEntries)[0];
            scope.Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", sessionCookie);

            await using (var updateContext = _fixture.CreateDbContext())
            {
                var user = await updateContext.BpUsers.SingleAsync(existingUser => existingUser.UserName == userName, cancellationToken);
                user.IsActive = false;
                await updateContext.SaveChangesAsync(cancellationToken);
            }

            using var protectedResponse = await scope.Client.GetAsync("/api/sport/list", cancellationToken);

            Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
            Assert.Contains(
                protectedResponse.Headers.GetValues("Set-Cookie"),
                cookie => cookie.StartsWith("SessionId=", StringComparison.Ordinal));
        }
        finally
        {
            await using var cleanupContext = _fixture.CreateDbContext();
            cleanupContext.BpUsers.RemoveRange(await cleanupContext.BpUsers
                .Where(user => user.UserName == userName)
                .ToListAsync(cancellationToken));
            await cleanupContext.SaveChangesAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task Admin_access_is_revoked_when_role_is_removed_during_active_session()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(cancellationToken);

        try
        {
            using var initialWrite = await scope.Client.PostAsJsonAsync(
                "/api/sport/add",
                new { Name = $"{scope.NamePrefix}before-role-removal", Active = true },
                cancellationToken);
            Assert.Equal(HttpStatusCode.OK, initialWrite.StatusCode);

            await using (var updateContext = _fixture.CreateDbContext())
            {
                var adminRole = await updateContext.BpUsers_BpRoles.SingleAsync(
                    userRole => userRole.UserId == BpUserConfiguration.AdminUserId
                        && userRole.RoleId == BpRoleConfiguration.AdminRoleId,
                    cancellationToken);
                updateContext.BpUsers_BpRoles.Remove(adminRole);
                await updateContext.SaveChangesAsync(cancellationToken);
            }

            using var forbiddenWrite = await scope.Client.PostAsJsonAsync(
                "/api/sport/add",
                new { Name = $"{scope.NamePrefix}after-role-removal", Active = true },
                cancellationToken);
            using var allowedRead = await scope.Client.GetAsync("/api/sport/list", cancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, forbiddenWrite.StatusCode);
            Assert.Equal(HttpStatusCode.OK, allowedRead.StatusCode);
        }
        finally
        {
            await using var cleanupContext = _fixture.CreateDbContext();
            if (!await cleanupContext.BpUsers_BpRoles.AnyAsync(
                userRole => userRole.UserId == BpUserConfiguration.AdminUserId
                    && userRole.RoleId == BpRoleConfiguration.AdminRoleId,
                cancellationToken))
            {
                cleanupContext.BpUsers_BpRoles.Add(new BpUserBpRole
                {
                    UserId = BpUserConfiguration.AdminUserId,
                    RoleId = BpRoleConfiguration.AdminRoleId,
                });
                await cleanupContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    [Fact]
    public async Task Check_reports_unauthenticated_and_protected_list_rejects_anonymous_request()
    {
        await using var scope = _fixture.CreateTestScope();

        using var checkResponse = await scope.Client.GetAsync("/api/auth/check", TestContext.Current.CancellationToken);
        var check = await checkResponse.Content.ReadFromJsonAsync<AuthCheckResponse>(TestContext.Current.CancellationToken);
        using var listResponse = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        Assert.NotNull(check);
        Assert.False(check.IsAuthenticated);
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
    }

    [Fact]
    public async Task AuthCookie_alone_does_not_authenticate_and_is_cleared()
    {
        await using var scope = _fixture.CreateTestScope();
        scope.Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", "AuthCookie=legacy-ticket");

        using var response = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("AuthCookie=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Expired_database_session_is_rejected_and_cookie_is_cleared()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var sessionId = scope.Cookies["SessionId"];
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var storedSession = await dbContext.BpUserSessions.SingleAsync(
                session => session.TokenHash == tokenHash,
                TestContext.Current.CancellationToken);
            storedSession.ExpiresDate = DateTime.UtcNow.AddMinutes(-1);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var response = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("SessionId=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Logout_revokes_database_session()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);
        var sessionId = scope.Cookies["SessionId"];
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));

        using var logoutResponse = await scope.Client.PostAsync(
            "/api/auth/logout",
            content: null,
            cancellationToken: TestContext.Current.CancellationToken);
        var logout = await logoutResponse.Content.ReadFromJsonAsync<ApiSuccessResponse>(TestContext.Current.CancellationToken);
        using var checkResponse = await scope.Client.GetAsync("/api/auth/check", TestContext.Current.CancellationToken);
        var check = await checkResponse.Content.ReadFromJsonAsync<AuthCheckResponse>(TestContext.Current.CancellationToken);
        await using var dbContext = _fixture.CreateDbContext();

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.NotNull(logout);
        Assert.True(logout.Success);
        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        Assert.NotNull(check);
        Assert.False(check.IsAuthenticated);
        Assert.False(await dbContext.BpUserSessions.AnyAsync(
            session => session.TokenHash == tokenHash,
            TestContext.Current.CancellationToken));
    }
}
