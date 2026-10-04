using System.Security.Claims;
using BetPortalWebApi.Contracts.List;
using BetPortalWebApi.Utils;
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
        private static readonly IReadOnlyDictionary<string, Func<IQueryable<Sport>, string, IQueryable<Sport>>> FilterHandlers =
            new Dictionary<string, Func<IQueryable<Sport>, string, IQueryable<Sport>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = static (query, value) =>
                {
                    var trimmedName = value.Trim();
                    return query.Where(s => EF.Functions.Like(s.Name, $"%{trimmedName}%"));
                },
                ["active"] = static (query, value) =>
                {
                    return bool.TryParse(value, out var parsed)
                        ? query.Where(s => s.Active == parsed)
                        : query;
                },
            };

        private static readonly IReadOnlyDictionary<string, Func<IQueryable<Sport>, bool, IQueryable<Sport>>> SortHandlers =
            new Dictionary<string, Func<IQueryable<Sport>, bool, IQueryable<Sport>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = static (query, isDescending) =>
                    isDescending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
                ["name"] = static (query, isDescending) =>
                    isDescending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
                ["active"] = static (query, isDescending) =>
                    isDescending
                        ? query.OrderByDescending(s => s.Active).ThenBy(s => s.Name)
                        : query.OrderBy(s => s.Active).ThenBy(s => s.Name),
            };

        private readonly BpDbContext _dbContext;

        public SportController(BpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetSportsList([FromQuery] ListQueryRequest request)
        {
            IQueryable<Sport> query = _dbContext.Sports.AsNoTracking();
            query = ListQueryProcessor.Apply(query, request, HttpContext.Request.Query, FilterHandlers, SortHandlers, defaultSortKey: "name");

            var sports = await query
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

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Sport name is required." });
            }

            var sport = await _dbContext.Sports.FindAsync(id);
            if (sport == null)
            {
                return NotFound(new { message = "Sport not found." });
            }

            var normalizedName = request.Name.Trim();
            var duplicateExists = await _dbContext.Sports.AnyAsync(s => s.Name == normalizedName && s.Id != id);
            if (duplicateExists)
            {
                return Conflict(new { message = "Another sport with this name already exists." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var modifiedByUserId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 1;

            sport.Name = normalizedName;
            sport.Active = request.Active;
            sport.ModifiedDate = DateTime.UtcNow;
            sport.ModifiedByUserId = modifiedByUserId;

            await _dbContext.SaveChangesAsync();

            return Ok(new SportDto
            {
                Id = sport.Id,
                Name = sport.Name,
                Active = sport.Active,
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var sport = await _dbContext.Sports.FindAsync(id);
            if (sport == null)
            {
                return NotFound(new { message = "Sport not found." });
            }

            _dbContext.Sports.Remove(sport);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        public class AddSportRequest
        {
            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; } = true;
        }

        public class UpdateSportRequest
        {
            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; }
        }

        public class SportDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; }
        }
    }
}
