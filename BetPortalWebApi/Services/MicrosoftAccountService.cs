using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;

namespace BetPortalWebApi.Services
{
    public sealed class MicrosoftAccountService(
        BpDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<MicrosoftAccountService> logger)
        : ExternalAccountService("Microsoft", dbContext, emailService, configuration, logger), IMicrosoftAccountService
    {
    }
}
