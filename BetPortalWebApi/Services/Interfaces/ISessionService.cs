using BetPortalWebApi.Services.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(Guid userId, TimeSpan expiry);

        Task<SessionData?> GetSessionAsync(string sessionId);

        Task RefreshSessionAsync(string sessionId, TimeSpan expiry);

        Task DeleteSessionAsync(string sessionId);
    }
}
