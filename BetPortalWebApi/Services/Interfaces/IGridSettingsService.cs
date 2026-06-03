using BetPortalWebApi.Contracts.GridSettings;

namespace BetPortalWebApi.Services.Interfaces
{
    public interface IGridSettingsService
    {
        Task<GridSettingsDto?> GetUserGridSettingsAsync(int userId, string pageIdentifier);

        Task<GridSettingsDto> SaveUserGridSettingsAsync(int userId, string pageIdentifier, GridSettingsData settings);

        Task<bool> DeleteUserGridSettingsAsync(int userId, string pageIdentifier);
    }
}
