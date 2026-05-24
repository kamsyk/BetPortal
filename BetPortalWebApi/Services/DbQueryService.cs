using System.Text;
using System.Text.Json;
using BpDbLayer;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public class DbQueryService : IDbQueryService
    {
        private readonly BpDbContext _dbContext;
        private readonly ILogger<DbQueryService> _logger;

        public DbQueryService(BpDbContext dbContext, ILogger<DbQueryService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<string> GetDatabaseContextAsync(string userMessage)
        {
            try
            {
                var context = new StringBuilder();
                var messageLower = userMessage.ToLowerInvariant();

                // Analyze the message to determine what data to fetch
                var needsSports = ContainsKeywords(messageLower, "sport", "sports", "game", "games");
                var needsUsers = ContainsKeywords(messageLower, "user", "users", "account", "accounts", "administrator", "admin");
                var needsRoles = ContainsKeywords(messageLower, "role", "roles", "permission", "permissions");
                var needsStats = ContainsKeywords(messageLower, "statistic", "statistics", "stat", "stats", "count", "total", "how many");

                // If no specific keywords, provide general overview
                if (!needsSports && !needsUsers && !needsRoles && !needsStats)
                {
                    needsStats = true; // Default to showing stats
                }

                // Fetch sports data if needed
                if (needsSports || needsStats)
                {
                    var sports = await _dbContext.Sports
                        .OrderBy(s => s.Name)
                        .ToListAsync();

                    context.AppendLine("=== Sports Data ===");
                    context.AppendLine($"Total sports: {sports.Count}");
                    context.AppendLine($"Active sports: {sports.Count(s => s.Active)}");
                    
                    if (needsSports && sports.Any())
                    {
                        context.AppendLine("\nSports list:");
                        foreach (var sport in sports.Take(20)) // Limit to avoid token overflow
                        {
                            context.AppendLine($"- {sport.Name} (ID: {sport.Id}, Active: {sport.Active})");
                        }
                        
                        if (sports.Count > 20)
                        {
                            context.AppendLine($"... and {sports.Count - 20} more sports");
                        }
                    }

                    context.AppendLine();
                }

                // Fetch users data if needed
                if (needsUsers || needsStats)
                {
                    var usersCount = await _dbContext.BpUsers.CountAsync();
                    var activeUsers = await _dbContext.BpUsers.CountAsync(u => u.IsActive);

                    context.AppendLine("=== Users Data ===");
                    context.AppendLine($"Total users: {usersCount}");
                    context.AppendLine($"Active users: {activeUsers}");
                    
                    if (needsUsers && usersCount > 0)
                    {
                        var recentUsers = await _dbContext.BpUsers
                            .OrderByDescending(u => u.Id)
                            .Take(10)
                            .Select(u => new { u.Id, u.UserName, u.Email, u.IsActive })
                            .ToListAsync();

                        context.AppendLine("\nRecent users:");
                        foreach (var user in recentUsers)
                        {
                            context.AppendLine($"- {user.UserName} (Email: {user.Email}, Active: {user.IsActive})");
                        }
                    }

                    context.AppendLine();
                }

                // Fetch roles data if needed
                if (needsRoles || needsStats)
                {
                    var roles = await _dbContext.BpRoles.ToListAsync();
                    var userRoleAssignments = await _dbContext.BpUsers_BpRoles.CountAsync();

                    context.AppendLine("=== Roles Data ===");
                    context.AppendLine($"Total roles: {roles.Count}");
                    context.AppendLine($"Total role assignments: {userRoleAssignments}");
                    
                    if (needsRoles && roles.Any())
                    {
                        context.AppendLine("\nAvailable roles:");
                        foreach (var role in roles)
                        {
                            var assignedCount = await _dbContext.BpUsers_BpRoles
                                .CountAsync(ur => ur.RoleId == role.Id);
                            context.AppendLine($"- {role.RoleName} ({assignedCount} users assigned)");
                        }
                    }

                    context.AppendLine();
                }

                // Add timestamp
                context.AppendLine($"Data retrieved at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

                return context.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching database context");
                return "Error: Unable to fetch database information at this time.";
            }
        }

        private bool ContainsKeywords(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
