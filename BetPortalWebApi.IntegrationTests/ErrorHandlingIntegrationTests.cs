using System.Net;
using BetPortalWebApi.Middleware;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[Collection("API integration tests")]
public sealed class ErrorHandlingIntegrationTests(ApiFixture fixture)
{
    private const string SensitiveDetail = "Sensitive SQL detail: Server=secret-host;Password=hunter2";

    [Fact]
    public async Task Unhandled_exception_returns_sanitized_problem_and_is_logged_to_database_with_error_id()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateClient(
            services => services.AddScoped<ISessionService, ThrowingSessionService>(),
            new Dictionary<string, string?> { ["DatabaseLogging:BatchPeriodSeconds"] = "0.2" });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/check");
        request.Headers.Add("Cookie", "SessionId=test-session");

        using var response = await client.SendAsync(request, cancellationToken);

        var body = await ExternalAuthAssert.SanitizedProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", body.GetProperty("title").GetString());
        var raw = body.GetRawText();
        Assert.DoesNotContain("secret-host", raw);
        Assert.DoesNotContain(nameof(InvalidOperationException), raw);
        var errorId = body.GetProperty("errorId").GetString()!;

        var log = await WaitForLogAsync(errorId, cancellationToken);
        Assert.Equal("Error", log.Level);
        Assert.Equal("/api/auth/check", log.RequestPath);
        Assert.Contains(SensitiveDetail, log.Exception);
        Assert.Equal("BetPortalWebApi.ErrorHandling.GlobalExceptionHandler", log.SourceContext);
    }

    private async Task<AppLogRow> WaitForLogAsync(string traceId, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            await using var dbContext = fixture.CreateDbContext();
            var rows = await dbContext.Database
                .SqlQuery<AppLogRow>($"SELECT [Level], [RequestPath], [Exception], [SourceContext] FROM [dbo].[BpAppLogs] WHERE [TraceId] = {traceId} AND [Exception] IS NOT NULL")
                .ToListAsync(cancellationToken);
            if (rows.Count > 0)
            {
                return Assert.Single(rows);
            }

            Assert.True(DateTime.UtcNow < deadline, $"No BpAppLogs row was written for trace {traceId}.");
            await Task.Delay(200, cancellationToken);
        }
    }

    private sealed record AppLogRow(string Level, string? RequestPath, string? Exception, string? SourceContext);

    private sealed class ThrowingSessionService : ISessionService
    {
        public Task<string> CreateSessionAsync(int userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);

        public Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);

        public Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);

        public Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);

        public Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);

        public Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveDetail);
    }
}
