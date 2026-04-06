using BetPortalWebApi.Middleware;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(int userId, string ipAddress, string userAgent);

        Task<UserSession?> GetSessionAsync(string sessionId);

        Task<bool> ValidateSessionAsync(string sessionId);

        Task RevokeSessionAsync(string sessionId);

        Task CleanupExpiredSessionsAsync();

        Task<List<string>> GetUserPermissionsAsync(int userId);
    }
}
