using BetPortalWebApi.Contracts.User;
using BpDbLayer.Models;
using KsAuthentication.Contracts;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IUserService
    {
        Task<JwtUser?> ValidateCredentialsAsync(string email, string password);

        Task<JwtUser?> GetByIdAsync(Guid userId);
    }
}
