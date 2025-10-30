using System.Text.Json;
using BetPortalWebApi.Services.Interfaces;
using BetPortalWebApi.Services.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace BetPortalWebApi.Services
{
    public class RedisSessionService : ISessionService
    {
        private static string? tmpSessionKey = null;

        ////private readonly IDatabase _database;
        private readonly IUserService _userService;

        public RedisSessionService(IUserService userService)
        {
            ////_database = database ?? throw new ArgumentNullException(nameof(database));
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

            ////await _database.StringSetAsync($"session:{sessionId}", JsonSerializer.Serialize(sessionData), expiry);

            tmpSessionKey = JsonSerializer.Serialize(sessionData);

            return sessionId;
        }

        public async Task<SessionData?> GetSessionAsync(string sessionId)
        {
            await Task.Delay(1); // Simulate async work
            ////var data = await _database.StringGetAsync($"session:{sessionId}");
            ////return data.HasValue ? JsonSerializer.Deserialize<SessionData>(data) : null;
            return tmpSessionKey != null ? JsonSerializer.Deserialize<SessionData>(tmpSessionKey) : null;
        }

        public async Task RefreshSessionAsync(string sessionId, TimeSpan expiry)
        {
            await Task.Delay(1); // Simulate async work
            ////await _database.KeyExpireAsync($"session:{sessionId}", expiry);
        }

        public async Task DeleteSessionAsync(string sessionId)
        {
            await Task.Delay(1); // Simulate async work
            ////await _database.KeyDeleteAsync($"session:{sessionId}");
            tmpSessionKey = null;
        }
    }
}
