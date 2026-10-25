using System.Security.Cryptography;
using System.Text.Json;
using BetPortalWebApi.ErrorHandling;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    /// <summary>
    /// Creates short-lived encrypted tokens describing how an existing account can sign in. The token keeps the email out of the
    /// portal URL and cannot be forged, so the login page can show trustworthy guidance.
    /// </summary>
    public class ExternalAuthErrorDetailsService(
        BpDbContext dbContext,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ExternalAuthErrorDetailsService> logger) : IExternalAuthErrorDetailsService
    {
        public const string PasswordMethod = "Password";

        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

        private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
            .CreateProtector("BetPortal.ExternalAuthErrorDetails.v1")
            .ToTimeLimitedDataProtector();

        public static string MaskEmail(string email)
        {
            var at = email.LastIndexOf('@');
            if (at <= 0)
            {
                return "***";
            }

            return string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(at));
        }

        public async Task<string> CreateTokenAsync(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = email.ToLowerInvariant();
            var accounts = await dbContext.BpUsers
                .AsNoTracking()
                .Where(user => user.Email.ToLower() == normalizedEmail)
                .Select(user => new
                {
                    HasPassword = user.Password != null && user.Password != string.Empty,
                    Providers = user.UserLoginProviders.Select(link => link.LoginProvider.ProviderName),
                })
                .ToListAsync(cancellationToken);

            var methods = new List<string>();
            if (accounts.Any(account => account.HasPassword))
            {
                methods.Add(PasswordMethod);
            }

            methods.AddRange(accounts
                .SelectMany(account => account.Providers)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase));

            var details = new ExternalAuthErrorDetails { Email = MaskEmail(email), SignInMethods = methods };
            return _protector.Protect(JsonSerializer.Serialize(details), Lifetime);
        }

        public ExternalAuthErrorDetails? Read(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ExternalAuthErrorDetails>(_protector.Unprotect(token));
            }
            catch (Exception exception) when (exception is CryptographicException or JsonException or FormatException)
            {
                logger.LogInformation("External sign-in error details token is invalid or expired.");
                return null;
            }
        }
    }
}
