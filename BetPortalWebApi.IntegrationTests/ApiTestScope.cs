using System.Net;
using System.Net.Http.Json;
using BetPortalWebApi.Controllers;
using BpDbLayer;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

internal sealed class ApiTestScope : IAsyncDisposable
{
    private readonly ApiFixture _fixture;
    private readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);

    public ApiTestScope(ApiFixture fixture, HttpClient client)
    {
        _fixture = fixture;
        Client = client;
        NamePrefix = $"it-{Guid.NewGuid():N}-";
    }

    public HttpClient Client { get; }

    public string NamePrefix { get; }

    public IReadOnlyDictionary<string, string> Cookies => _cookies;

    public async Task<LoginResponse> LoginAsAdminAsync(CancellationToken cancellationToken)
    {
        using var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Username = "admin",
            Password = "admin",
        }, cancellationToken);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(login);
        Assert.True(login.Success);

        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            throw new InvalidOperationException("Successful login did not return authentication cookies.");
        }

        foreach (var setCookieHeader in setCookieHeaders)
        {
            var cookiePair = setCookieHeader.Split(';', 2, StringSplitOptions.TrimEntries)[0];
            var separatorIndex = cookiePair.IndexOf('=');
            if (separatorIndex > 0)
            {
                _cookies[cookiePair[..separatorIndex]] = cookiePair[(separatorIndex + 1)..];
            }
        }

        UseCookies("SessionId");
        return login;
    }

    public void UseCookies(params string[] cookieNames)
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        if (cookieNames.Length == 0)
        {
            return;
        }

        var cookieValues = cookieNames.Select(cookieName =>
        {
            if (!_cookies.TryGetValue(cookieName, out var value))
            {
                throw new InvalidOperationException($"The login response did not contain a {cookieName} cookie.");
            }

            return $"{cookieName}={value}";
        });

        Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", string.Join("; ", cookieValues));
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        await using var dbContext = _fixture.CreateDbContext();
        var createdSports = await dbContext.Sports
            .Where(sport => sport.Name.StartsWith(NamePrefix))
            .ToListAsync();
        dbContext.Sports.RemoveRange(createdSports);
        await dbContext.SaveChangesAsync();
    }
}