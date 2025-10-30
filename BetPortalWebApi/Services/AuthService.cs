using BetPortalWebApi.Services.Interfaces;
using KsAuthentication.Contracts;

namespace BetPortalWebApi.Services
{
    public class AuthService : IAuthService
    {
        ////private readonly UserService _userService

        /*public AuthService(UserService userService)
        {
            _userService = userService;
        }*/

        public async Task<JwtUser?> ValidateCredentialsAsync(string email, string password)
        {
            await Task.Delay(1); // Simulate async work
                                 ////var user = await _userService.GetUserByEmailAsync(email);

            /*
            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }*/

            if (email != "test@local")
           {
               return null;
           }

            if (password != "Pass@123")
            {
                return null;
            }

            return new JwtUser
            {
                Email = email,
                UserName = email,
            };
        }
    }
}
