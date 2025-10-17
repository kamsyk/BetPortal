namespace BetPortalWebApi.Services
{
    public interface IUserService
    {
        Task<AppUser?> ValidateCredentialsAsync(string email, string password);
    }
    }
