using System.Security.Claims;
using BetPortalWebApi.Contracts.GridSettings;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GridSettingsController : BpBaseController
    {
        private readonly IGridSettingsService _gridSettingsService;

        public GridSettingsController(IGridSettingsService gridSettingsService)
        {
            _gridSettingsService = gridSettingsService;
        }

        [HttpGet("{pageIdentifier}")]
        public async Task<IActionResult> GetSettings(string pageIdentifier)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID." });
            }

            var settings = await _gridSettingsService.GetUserGridSettingsAsync(userId, pageIdentifier);

            if (settings == null)
            {
                return NotFound();
            }

            return Ok(settings);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] SaveGridSettingsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PageIdentifier))
            {
                return BadRequest(new { message = "Page identifier is required." });
            }

            if (request.Settings == null)
            {
                return BadRequest(new { message = "Settings are required." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID." });
            }

            var savedSettings = await _gridSettingsService.SaveUserGridSettingsAsync(
                userId,
                request.PageIdentifier,
                request.Settings);

            return Ok(savedSettings);
        }

        [HttpDelete("{pageIdentifier}")]
        public async Task<IActionResult> DeleteSettings(string pageIdentifier)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user ID." });
            }

            var deleted = await _gridSettingsService.DeleteUserGridSettingsAsync(userId, pageIdentifier);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
