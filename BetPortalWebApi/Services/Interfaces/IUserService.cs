using BetPortalWebApi.Contracts.User;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<BpUserDto?> ValidateCredentialsAsync(string email, string password);
    }
}
