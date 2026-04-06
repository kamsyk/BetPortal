using System.Security.Claims;
using BpDbLayer;
using BpDbLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SportController : BpBaseController
    {
        private readonly BpDbContext _dbContext;

        public SportController(BpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetSportsList()
        {
            var sports = await _dbContext.Sports
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new SportDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Active = s.Active,
                })
                .ToListAsync();

            return Ok(sports);
        }

        [HttpPost("add")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> AddNew([FromBody] AddSportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Sport name is required." });
            }

            var normalizedName = request.Name.Trim();
            var alreadyExists = await _dbContext.Sports.AnyAsync(s => s.Name == normalizedName);
            if (alreadyExists)
            {
                return Conflict(new { message = "Sport already exists." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var modifiedByUserId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 1;

            var sport = new Sport
            {
                Name = normalizedName,
                Active = request.Active,
                ModifiedDate = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId,
            };

            _dbContext.Sports.Add(sport);
            await _dbContext.SaveChangesAsync();

            return Ok(new SportDto
            {
                Id = sport.Id,
                Name = sport.Name,
                Active = sport.Active,
            });
        }

        public class AddSportRequest
        {
            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; } = true;
        }

        public class SportDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; }
        }
    }
}
