using BetPortalWebApi.Middleware;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(int userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default);

        Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

        Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);

        Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default);
    }
}
