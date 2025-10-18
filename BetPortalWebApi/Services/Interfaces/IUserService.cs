using BetPortalWebApi.Contracts.User;
using KsAuthentication.Contracts;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<JwtUser?> ValidateCredentialsAsync(string email, string password);
    }
}
