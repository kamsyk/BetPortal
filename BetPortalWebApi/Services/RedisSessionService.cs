using System.Collections.Concurrent;
using BetPortalWebApi.Services.Interfaces;
using BetPortalWebApi.Services.Models;

namespace BetPortalWebApi.Services
{
    public class RedisSessionService : ISessionService
    {
        private readonly ConcurrentDictionary<string, SessionData> _sessions = new ();
        private readonly IUserService _userService;

        public RedisSessionService(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<string> CreateSessionAsync(Guid userId, TimeSpan expiry)
        {
            var sessionId = Guid.NewGuid().ToString();
            var user = await _userService.GetByIdAsync(userId);

            var sessionData = new SessionData
            {
                UserId = userId,
                Email = user!.Email,
                Name = user.UserName,
                ExpiresAt = DateTime.UtcNow.Add(expiry),
                CreatedAt = DateTime.UtcNow,
            };

            _sessions[sessionId] = sessionData;

            return sessionId;
        }

        public Task<SessionData?> GetSessionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult<SessionData?>(null);
            }

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _sessions.TryRemove(sessionId, out _);
                return Task.FromResult<SessionData?>(null);
            }

            return Task.FromResult<SessionData?>(session);
        }

        public Task RefreshSessionAsync(string sessionId, TimeSpan expiry)
        {
            if (_sessions.TryGetValue(sessionId, out var session) && session.ExpiresAt >= DateTime.UtcNow)
            {
                session.ExpiresAt = DateTime.UtcNow.Add(expiry);
            }

            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _sessions.TryRemove(sessionId, out _);
            }

            return Task.CompletedTask;
        }
    }
}
