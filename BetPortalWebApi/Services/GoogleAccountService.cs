using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;

namespace BetPortalWebApi.Services
{
    public sealed class GoogleAccountService(
        BpDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<GoogleAccountService> logger)
        : ExternalAccountService("Google", dbContext, emailService, configuration, logger), IGoogleAccountService
    {
    }
}
