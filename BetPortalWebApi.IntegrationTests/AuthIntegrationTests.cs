using System.Net;
using System.Net.Http.Json;

[Collection("API integration tests")]
public sealed class AuthIntegrationTests
{
    private readonly ApiFixture _fixture;

    public AuthIntegrationTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_sets_both_cookies_and_check_reports_authenticated()
    {
        await using var scope = _fixture.CreateTestScope();

        var login = await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        Assert.True(login.Success);
        Assert.Equal("admin", login.User.Username);
        Assert.Contains("SessionId", scope.Cookies.Keys);
        Assert.Contains("AuthCookie", scope.Cookies.Keys);

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
    public async Task Dynamic_auth_accepts_session_only_and_auth_cookie_only_requests()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        scope.UseCookies("SessionId");
        using var sessionResponse = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);

        scope.UseCookies("AuthCookie");
        using var cookieResponse = await scope.Client.GetAsync("/api/sport/list", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cookieResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_session_used_by_session_only_authentication()
    {
        await using var scope = _fixture.CreateTestScope();
        await scope.LoginAsAdminAsync(TestContext.Current.CancellationToken);

        using var logoutResponse = await scope.Client.PostAsync("/api/auth/logout", content: null, cancellationToken: TestContext.Current.CancellationToken);
        var logout = await logoutResponse.Content.ReadFromJsonAsync<ApiSuccessResponse>(TestContext.Current.CancellationToken);
        scope.UseCookies("SessionId");
        using var checkResponse = await scope.Client.GetAsync("/api/auth/check", TestContext.Current.CancellationToken);
        var check = await checkResponse.Content.ReadFromJsonAsync<AuthCheckResponse>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.NotNull(logout);
        Assert.True(logout.Success);
        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        Assert.NotNull(check);
        Assert.False(check.IsAuthenticated);
    }
}