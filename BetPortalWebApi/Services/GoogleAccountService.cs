using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public sealed class GoogleAccountService(BpDbContext dbContext, ILogger<GoogleAccountService> logger) : IGoogleAccountService
    {
        public async Task<(BpUser? User, bool AccountExists)> FindExistingAsync(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.ToLowerInvariant();
            var matches = await dbContext.BpUsers
                .AsNoTracking()
                .Where(user => user.Email.ToLower() == normalizedEmail)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (matches.Count > 1 || (matches.Count == 1 && !matches[0].IsActive))
            {
                logger.LogWarning("Google sign-in rejected because the local account is inactive or ambiguous.");
                return (null, true);
            }

            if (matches.Count == 1)
            {
                return (matches[0], true);
            }

            logger.LogInformation("Google sign-in requires an existing local account.");
            return (null, false);
        }
    }
}
