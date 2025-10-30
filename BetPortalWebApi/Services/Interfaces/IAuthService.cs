using KsAuthentication.Contracts;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IAuthService
    {
        Task<JwtUser?> ValidateCredentialsAsync(string email, string password);
        /*Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);*/
    }
}
