using BetPortalWebApi.ErrorHandling;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IExternalAuthErrorDetailsService
    {
        Task<string> CreateTokenAsync(string email, CancellationToken cancellationToken);

        ExternalAuthErrorDetails? Read(string? token);
    }
}
