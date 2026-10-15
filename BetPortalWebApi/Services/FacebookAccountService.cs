using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;

namespace BetPortalWebApi.Services
{
    public sealed class FacebookAccountService(
        BpDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<FacebookAccountService> logger)
        : ExternalAccountService("Facebook", dbContext, emailService, configuration, logger), IFacebookAccountService
    {
    }
}
