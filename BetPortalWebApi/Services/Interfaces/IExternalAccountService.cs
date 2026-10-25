using BetPortalWebApi.Services;
using BpDbLayer.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IExternalAccountService
    {
        Task<(BpUser? User, ExternalAccountStatus Status)> FindExistingAsync(string email, CancellationToken cancellationToken);

        Task<(bool AccountCreated, bool EmailSent)> RegisterAsync(
            string email, string? firstName, string? surname, CancellationToken cancellationToken);
    }
}
