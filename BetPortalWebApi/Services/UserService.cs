using BetPortalWebApi.Contracts.User;
using BetPortalWebApi.Services.Interfaces;
using KsAuthentication.Contracts;

namespace BetPortalWebApi.Services
{
    public class UserService : BpBaseService, IUserService
    {
        private static readonly JwtUser Demo = new ()
        {
            UserName = "test",
            Email = "test@local",
            Roles = ["User"],
        };

        public Task<JwtUser?> ValidateCredentialsAsync(string email, string password)
        {
            if (email.Equals("test@local", StringComparison.OrdinalIgnoreCase) && password == "Pass@123")
            {
                return Task.FromResult<JwtUser?>(Demo);
            }

            return Task.FromResult<JwtUser?>(null);
        }
    }
}
