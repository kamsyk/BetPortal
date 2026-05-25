using System.Text.Json;
using BpDbLayer;
using BpDbLayer.Models;
using BetPortalWebApi.Contracts.GridSettings;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public class GridSettingsService : BpBaseService, IGridSettingsService
    {
        private readonly BpDbContext _dbContext;

        public GridSettingsService(BpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GridSettingsDto?> GetUserGridSettingsAsync(int userId, string pageIdentifier)
        {
            var settings = await _dbContext.UserGridSettings
                .AsNoTracking()
                .Where(ugs => ugs.UserId == userId && ugs.PageIdentifier == pageIdentifier)
                .Select(ugs => new GridSettingsDto
                {
                    Id = ugs.Id,
                    UserId = ugs.UserId,
                    PageIdentifier = ugs.PageIdentifier,
                    SettingsJson = ugs.SettingsJson,
                    ModifiedDate = ugs.ModifiedDate,
                })
                .FirstOrDefaultAsync();

            return settings;
        }

        public async Task<GridSettingsDto> SaveUserGridSettingsAsync(int userId, string pageIdentifier, GridSettingsData settings)
        {
            var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });

            var existingSettings = await _dbContext.UserGridSettings
                .Where(ugs => ugs.UserId == userId && ugs.PageIdentifier == pageIdentifier)
                .FirstOrDefaultAsync();

            if (existingSettings != null)
            {
                existingSettings.SettingsJson = settingsJson;
                existingSettings.ModifiedDate = DateTime.UtcNow;
                existingSettings.ModifiedByUserId = userId;
            }
            else
            {
                existingSettings = new UserGridSettings
                {
                    UserId = userId,
                    PageIdentifier = pageIdentifier,
                    SettingsJson = settingsJson,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = userId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = userId,
                };

                _dbContext.UserGridSettings.Add(existingSettings);
            }

            await _dbContext.SaveChangesAsync();

            return new GridSettingsDto
            {
                Id = existingSettings.Id,
                UserId = existingSettings.UserId,
                PageIdentifier = existingSettings.PageIdentifier,
                SettingsJson = existingSettings.SettingsJson,
                ModifiedDate = existingSettings.ModifiedDate,
            };
        }

        public async Task<bool> DeleteUserGridSettingsAsync(int userId, string pageIdentifier)
        {
            var existingSettings = await _dbContext.UserGridSettings
                .Where(ugs => ugs.UserId == userId && ugs.PageIdentifier == pageIdentifier)
                .FirstOrDefaultAsync();

            if (existingSettings == null)
            {
                return false;
            }

            _dbContext.UserGridSettings.Remove(existingSettings);
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
