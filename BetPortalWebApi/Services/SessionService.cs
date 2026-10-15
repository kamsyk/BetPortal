using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BetPortalWebApi.Middleware;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BetPortalWebApi.Services
{
    public class SessionService : ISessionService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly BpDbContext _dbContext;
        private readonly ILogger<SessionService> _logger;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromHours(24);

        public SessionService(IMemoryCache memoryCache, BpDbContext dbContext, ILogger<SessionService> logger)
        {
            _memoryCache = memoryCache;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> CreateSessionAsync(int userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
        {
            //// Todo: KSY - remove it
            /* // Get user with permissions from database
             var user = await _dbContext.BpUsers
                 .Include(u => u.Permissions)
                 .ThenInclude(up => up.Permission)
                 .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);*/

            var user = await _dbContext.BpUsers.Where(u => u.Id == userId && u.IsActive).FirstOrDefaultAsync(cancellationToken);
            if (user == null)
            {
                 throw new UnauthorizedAccessException("User not found or inactive");
            }

            // Generate secure session ID
            var sessionId = GenerateSessionId();
            var expiresAt = DateTime.UtcNow.Add(_sessionTimeout);

            var permissions = await GetUserPermissionsAsync(userId, cancellationToken);

            // Create session object
            var session = new UserSession
            {
                SessionId = sessionId,
                UserId = user.Id,
                Username = user.UserName,
                FullName = user.FirstName + " " + user.Surname,
                Permissions = permissions,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
            };

            // Store in memory cache with sliding expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.High,
            };

            _memoryCache.Set($"session_{sessionId}", session, cacheOptions);

            _logger.LogInformation("Session created for user {UserId} with ID {SessionId}", userId, sessionId);

            return sessionId;
        }

        public async Task<UserSession?> GetSessionAsync(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }

            // Try to get from cache first
            if (_memoryCache.TryGetValue($"session_{sessionId}", out UserSession? cachedSession))
            {
                // Check if session is still valid
                if (cachedSession != null && cachedSession.ExpiresAt > DateTime.UtcNow)
                {
                    // Refresh permissions from database periodically (every 10 minutes)
                    if (DateTime.UtcNow.Subtract(cachedSession.CreatedAt).TotalMinutes > 10)
                    {
                        var refreshedPermissions = await GetUserPermissionsAsync(cachedSession.UserId);
                        cachedSession.Permissions = refreshedPermissions;

                        // Update cache
                        var cacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpiration = cachedSession.ExpiresAt,
                            SlidingExpiration = TimeSpan.FromMinutes(30),
                            Priority = CacheItemPriority.High,
                        };
                        _memoryCache.Set($"session_{sessionId}", cachedSession, cacheOptions);
                    }

                    return cachedSession;
                }
                else
                {
                    // Session expired, remove from cache
                    _memoryCache.Remove($"session_{sessionId}");
                }
            }

            return null;
        }

        public async Task<bool> ValidateSessionAsync(string sessionId)
        {
            var session = await GetSessionAsync(sessionId);
            return session != null;
        }

        public async Task RevokeSessionAsync(string sessionId)
        {
            if (!string.IsNullOrEmpty(sessionId))
            {
                _memoryCache.Remove($"session_{sessionId}");
                _logger.LogInformation("Session {SessionId} revoked", sessionId);
            }

            await Task.CompletedTask;
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            // This is handled automatically by memory cache expiration
            // In a distributed cache scenario, you'd implement periodic cleanup
            _logger.LogDebug("Session cleanup completed (handled by memory cache)");
            await Task.CompletedTask;
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

        private string GenerateSessionId()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", string.Empty);
        }
    }
}