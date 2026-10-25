using System.Data;
using System.Net;
using System.Security.Cryptography;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public abstract class ExternalAccountService(
        string providerName,
        BpDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger logger)
    {
        public async Task<(bool AccountCreated, bool EmailSent)> RegisterAsync(
            string email, string? firstName, string? surname, CancellationToken cancellationToken)
        {
            var origin = new Uri(configuration[$"Authentication:{providerName}:FrontendOrigin"]
                ?? throw new InvalidOperationException($"The {providerName} frontend origin is not configured."));
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();
            if (await dbContext.BpUsers.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken)
                || await dbContext.BpUserLoginProviders.AnyAsync(
                    link => link.LoginProvider.ProviderName == providerName && link.Email.ToLower() == normalizedEmail, cancellationToken))
            {
                logger.LogWarning("{Provider} registration rejected because the local account or provider link already exists.", providerName);
                return (false, false);
            }

            var loginProvider = await dbContext.LoginProviders.SingleOrDefaultAsync(
                provider => provider.ProviderName == providerName, cancellationToken)
                ?? throw new InvalidOperationException($"The {providerName} login provider is missing. Apply the login provider migration before registering users.");
            var now = DateTime.UtcNow;
            var user = new BpUser
            {
                UserName = $"{providerName.ToLowerInvariant()}-{Guid.NewGuid():N}",
                Email = email,
                FirstName = firstName is { Length: > 100 } ? firstName[..100] : firstName,
                Surname = surname is { Length: > 100 } ? surname[..100] : surname,
                IsActive = false,
                CreatedDate = now,
                ModifiedDate = now,
                CreatedByUserId = BpUserConfiguration.AdminUserId,
                ModifiedByUserId = BpUserConfiguration.AdminUserId,
            };
            user.UserToken = new BpUserToken
            {
                User = user,
                UserInitToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                CreatedDate = now,
            };
            user.UserLoginProviders.Add(new BpUserLoginProvider
            {
                User = user,
                LoginProvider = loginProvider,
                Email = email,
            });
            dbContext.BpUsers.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var activationUrl = new Uri(origin, "/user-token").AbsoluteUri
                + "?token=" + Uri.EscapeDataString(user.UserToken.UserInitToken);
            var greeting = string.IsNullOrWhiteSpace(user.FirstName) ? "Hello" : $"Hello {user.FirstName}";
            var htmlContent = $"<h1>Welcome to Bet Portal</h1><p>{WebUtility.HtmlEncode(greeting)},</p>"
                + "<p>Thank you for registering. Activate your account using the link below:</p>"
                + $"<p><a href=\"{WebUtility.HtmlEncode(activationUrl)}\">Activate your account</a></p>"
                + "<p>If you did not register, you can ignore this email.</p>";
            var plainTextContent = $"Welcome to Bet Portal\n\n{greeting},\n\n"
                + $"Thank you for registering. Activate your account:\n{activationUrl}\n\n"
                + "If you did not register, you can ignore this email.";
            var emailSent = await emailService.SendEmailAsync(
                user.Email, "Welcome to Bet Portal", htmlContent, plainTextContent, cancellationToken: cancellationToken);
            if (!emailSent)
            {
                logger.LogError("{Provider} registration completed, but the welcome email could not be sent.", providerName);
            }

            return (true, emailSent);
        }

        public async Task<(BpUser? User, ExternalAccountStatus Status)> FindExistingAsync(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.ToLowerInvariant();
            var matches = await dbContext.BpUserLoginProviders
                .AsNoTracking()
                .Where(link => link.LoginProvider.ProviderName == providerName && link.Email.ToLower() == normalizedEmail)
                .Select(link => link.User)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (matches.Count > 1)
            {
                logger.LogWarning("{Provider} sign-in rejected because the provider email is linked to multiple accounts.", providerName);
                return (null, ExternalAccountStatus.Ambiguous);
            }

            if (matches.Count == 1 && !matches[0].IsActive)
            {
                logger.LogWarning("{Provider} sign-in rejected because the linked account {UserId} is inactive.", providerName, matches[0].Id);
                return (null, ExternalAccountStatus.Inactive);
            }

            if (matches.Count == 1)
            {
                return (matches[0], ExternalAccountStatus.Found);
            }

            if (await dbContext.BpUsers.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken))
            {
                logger.LogWarning("{Provider} sign-in rejected because the local account has no matching provider link.", providerName);
                return (null, ExternalAccountStatus.NotLinked);
            }

            logger.LogInformation("{Provider} sign-in requires an existing local account.", providerName);
            return (null, ExternalAccountStatus.NotFound);
        }
    }
}