using BetPortalWebApi.Contracts.User;
using BetPortalWebApi.Services.Interfaces;

namespace BetPortalWebApi.Services
{
    public class UserService : BpBaseService, IUserService
    {
        private static readonly BpUserDto Demo = new BpUserDto
        {
            UserName = "test",
            Email = "test@local",
            Roles = new[] { "User" },
        };

        public Task<BpUserDto?> ValidateCredentialsAsync(string email, string password)
        {
            if (email.Equals("test@local", StringComparison.OrdinalIgnoreCase) && password == "Pass@123")
            {
                return Task.FromResult<BpUserDto?>(Demo);
            }

            return Task.FromResult<BpUserDto?>(null);
        }
    }
}
