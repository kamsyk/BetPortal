using BpDbLayer.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IGoogleAccountService
    {
        Task<(BpUser? User, bool AccountExists)> FindExistingAsync(string email, CancellationToken cancellationToken);
    }
}
