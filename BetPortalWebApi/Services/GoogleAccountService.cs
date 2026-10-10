using System.Data;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public sealed class GoogleAccountService(BpDbContext dbContext, ILogger<GoogleAccountService> logger) : IGoogleAccountService
    {
        public async Task<BpUser?> FindOrCreateAsync(string email, string? firstName, string? surname, CancellationToken cancellationToken)
        {
            // Serialize lookup and creation because the existing user table has no unique email index.
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();
            var matches = await dbContext.BpUsers
                .Where(user => user.Email.ToLower() == normalizedEmail)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (matches.Count > 1 || (matches.Count == 1 && !matches[0].IsActive))
            {
                logger.LogWarning("Google sign-in rejected because the local account is inactive or ambiguous.");
                return null;
            }

            if (matches.Count == 1)
            {
                await transaction.CommitAsync(cancellationToken);
                return matches[0];
            }

            var now = DateTime.UtcNow;
            var user = new BpUser
            {
                UserName = $"google-{Guid.NewGuid():N}",
                Email = email,
                FirstName = firstName is { Length: > 100 } ? firstName[..100] : firstName,
                Surname = surname is { Length: > 100 } ? surname[..100] : surname,
                IsActive = true,
                CreatedDate = now,
                ModifiedDate = now,
                CreatedByUserId = BpUserConfiguration.AdminUserId,
                ModifiedByUserId = BpUserConfiguration.AdminUserId,
            };
            dbContext.BpUsers.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return user;
        }
    }
}
