using BpDbLayer.Models;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IGoogleAccountService
    {
        Task<BpUser?> FindOrCreateAsync(string email, string? firstName, string? surname, CancellationToken cancellationToken);
    }
}
