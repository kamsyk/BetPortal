using System.Data;
using System.Security.Cryptography;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public sealed class GoogleAccountService(BpDbContext dbContext, ILogger<GoogleAccountService> logger) : IGoogleAccountService
    {
        public async Task<bool> RegisterAsync(string email, string? firstName, string? surname, CancellationToken cancellationToken)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();
            if (await dbContext.BpUsers.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken))
            {
                logger.LogWarning("Google registration rejected because the local account already exists.");
                return false;
            }

            var now = DateTime.UtcNow;
            var user = new BpUser
            {
                UserName = $"google-{Guid.NewGuid():N}",
                Email = email,
                FirstName = firstName is { Length: > 100 } ? firstName[..100] : firstName,
                Surname = surname is { Length: > 100 } ? surname[..100] : surname,
                IsActive = false,
                CreatedDate = now,
                ModifiedDate = now,
                CreatedByUserId = BpUserConfiguration.AdminUserId,
                ModifiedByUserId = BpUserConfiguration.AdminUserId,
            };
            user.UserToken = new UserToken
            {
                User = user,
                UserInitToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                CreatedDate = now,
            };
            dbContext.BpUsers.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

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