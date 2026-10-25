using System.Security.Cryptography;
using System.Text;
using BetPortalWebApi.Middleware;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public class SessionService : ISessionService
    {
        public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(24);

        private const int SessionTokenLength = 43;
        private readonly BpDbContext _dbContext;
        private readonly ILogger<SessionService> _logger;

        public SessionService(BpDbContext dbContext, ILogger<SessionService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> CreateSessionAsync(
            int userId,
            string ipAddress,
            string userAgent,
            CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.BpUsers
                .SingleOrDefaultAsync(existingUser => existingUser.Id == userId && existingUser.IsActive, cancellationToken);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            var sessionId = GenerateSessionId();
            var createdAt = DateTime.UtcNow;
            _dbContext.BpUserSessions.Add(new BpUserSession
            {
                UserId = user.Id,
                TokenHash = HashSessionId(sessionId),
                CreatedDate = createdAt,
                ExpiresDate = createdAt.Add(SessionLifetime),
                IpAddress = ipAddress.Length <= 45 ? ipAddress : ipAddress[..45],
                UserAgent = userAgent.Length <= 512 ? userAgent : userAgent[..512],
                CreatedByUserId = user.Id,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Session created for user {UserId}", userId);
            return sessionId;
        }

        public async Task<UserSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!IsValidSessionId(sessionId))
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var storedSession = await _dbContext.BpUserSessions
                .Include(session => session.User)
                .SingleOrDefaultAsync(
                    session => session.TokenHash == HashSessionId(sessionId)
                        && session.ExpiresDate > now
                        && session.User.IsActive,
                    cancellationToken);
            if (storedSession == null)
            {
                return null;
            }

            var permissions = await GetUserPermissionsAsync(storedSession.UserId, cancellationToken);
            var user = storedSession.User;
            return new UserSession
            {
                SessionId = sessionId,
                UserId = user.Id,
                Username = user.UserName,
                FullName = user.FirstName + " " + user.Surname,
                Permissions = permissions,
                IpAddress = storedSession.IpAddress,
                UserAgent = storedSession.UserAgent,
                CreatedAt = storedSession.CreatedDate,
                ExpiresAt = storedSession.ExpiresDate,
            };
        }

        public async Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await GetSessionAsync(sessionId, cancellationToken) != null;
        }

        public async Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (!IsValidSessionId(sessionId))
            {
                return;
            }

            var tokenHash = HashSessionId(sessionId);
            var deleted = await _dbContext.BpUserSessions
                .Where(session => session.TokenHash == tokenHash)
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted > 0)
            {
                _logger.LogInformation("Session revoked.");
            }
        }

        public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
        {
            var deleted = await _dbContext.BpUserSessions
                .Where(session => session.ExpiresDate <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
            _logger.LogDebug("Removed {SessionCount} expired sessions.", deleted);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var isAdmin = await _dbContext.BpUsers_BpRoles.AnyAsync(
                userRole => userRole.UserId == userId
                    && userRole.User.IsActive
                    && userRole.RoleId == BpRoleConfiguration.AdminRoleId
                    && userRole.Role.IsActive,
                cancellationToken);
            return isAdmin ? ["AdminAccess"] : [];
        }

        private static string GenerateSessionId()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        private static string HashSessionId(string sessionId)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)));
        }

        private static bool IsValidSessionId(string? sessionId)
        {
            return sessionId is { Length: SessionTokenLength }
                && sessionId.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
        }
    }
}
