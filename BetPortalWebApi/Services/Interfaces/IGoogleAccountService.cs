using BpDbLayer.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IGoogleAccountService
    {
        Task<(BpUser? User, bool AccountExists)> FindExistingAsync(string email, CancellationToken cancellationToken);

        Task<bool> RegisterAsync(string email, string? firstName, string? surname, CancellationToken cancellationToken);
    }
}
