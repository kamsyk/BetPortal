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
    public sealed class GoogleAccountService(
        BpDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<GoogleAccountService> logger) : IGoogleAccountService
    {
        public async Task<(bool AccountCreated, bool EmailSent)> RegisterAsync(
            string email, string? firstName, string? surname, CancellationToken cancellationToken)
        {
            var origin = new Uri(configuration["Authentication:Google:FrontendOrigin"]
                ?? throw new InvalidOperationException("The Google frontend origin is not configured."));
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var normalizedEmail = email.ToLowerInvariant();
            if (await dbContext.BpUsers.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken)
                || await dbContext.BpUserLoginProviders.AnyAsync(
                    link => link.LoginProvider.ProviderName == "Google" && link.Email.ToLower() == normalizedEmail, cancellationToken))
            {
                logger.LogWarning("Google registration rejected because the local account or Google provider link already exists.");
                return (false, false);
            }

            var googleProvider = await dbContext.LoginProviders.SingleOrDefaultAsync(
                provider => provider.ProviderName == "Google", cancellationToken)
                ?? throw new InvalidOperationException("The Google login provider is missing. Apply the login provider migration before registering users.");
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
            user.UserLoginProviders.Add(new BpUserLoginProvider
            {
                User = user,
                LoginProvider = googleProvider,
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
                logger.LogError("Google registration completed, but the welcome email could not be sent.");
            }

            return (true, emailSent);
        }

        public async Task<(BpUser? User, bool AccountExists)> FindExistingAsync(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.ToLowerInvariant();
            var matches = await dbContext.BpUserLoginProviders
                .AsNoTracking()
                .Where(link => link.LoginProvider.ProviderName == "Google" && link.Email.ToLower() == normalizedEmail)
                .Select(link => link.User)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (matches.Count > 1 || (matches.Count == 1 && !matches[0].IsActive))
            {
                logger.LogWarning("Google sign-in rejected because the linked account is inactive or the provider email is ambiguous.");
                return (null, true);
            }

            if (matches.Count == 1)
            {
                return (matches[0], true);
            }

            if (await dbContext.BpUsers.AnyAsync(user => user.Email.ToLower() == normalizedEmail, cancellationToken))
            {
                logger.LogWarning("Google sign-in rejected because the local account has no matching Google provider link.");
                return (null, true);
            }

            logger.LogInformation("Google sign-in requires an existing local account.");
            return (null, false);
        }
    }
}