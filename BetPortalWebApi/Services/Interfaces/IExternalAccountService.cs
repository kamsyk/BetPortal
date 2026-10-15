using BpDbLayer.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IExternalAccountService
    {
        Task<(BpUser? User, bool AccountExists)> FindExistingAsync(string email, CancellationToken cancellationToken);

        Task<(bool AccountCreated, bool EmailSent)> RegisterAsync(
            string email, string? firstName, string? surname, CancellationToken cancellationToken);
    }
}
