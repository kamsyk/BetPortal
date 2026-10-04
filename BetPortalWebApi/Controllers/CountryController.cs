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
    public class CountryController : BpBaseController
    {
        private static readonly IReadOnlyDictionary<string, Func<IQueryable<Country>, string, IQueryable<Country>>> FilterHandlers =
            new Dictionary<string, Func<IQueryable<Country>, string, IQueryable<Country>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["name"] = static (query, value) =>
                {
                    var trimmedName = value.Trim();
                    return query.Where(c => EF.Functions.Like(c.Name, $"%{trimmedName}%"));
                },
                ["active"] = static (query, value) =>
                {
                    return bool.TryParse(value, out var parsed)
                        ? query.Where(c => c.Active == parsed)
                        : query;
                },
            };

        private static readonly IReadOnlyDictionary<string, Func<IQueryable<Country>, bool, IQueryable<Country>>> SortHandlers =
            new Dictionary<string, Func<IQueryable<Country>, bool, IQueryable<Country>>>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = static (query, isDescending) =>
                    isDescending ? query.OrderByDescending(c => c.Id) : query.OrderBy(c => c.Id),
                ["name"] = static (query, isDescending) =>
                    isDescending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
                ["active"] = static (query, isDescending) =>
                    isDescending
                        ? query.OrderByDescending(c => c.Active).ThenBy(c => c.Name)
                        : query.OrderBy(c => c.Active).ThenBy(c => c.Name),
            };

        private readonly BpDbContext _dbContext;

        public CountryController(BpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCountriesList([FromQuery] ListQueryRequest request)
        {
            IQueryable<Country> query = _dbContext.Countries.AsNoTracking();
            query = ListQueryProcessor.Apply(query, request, HttpContext.Request.Query, FilterHandlers, SortHandlers, defaultSortKey: "name");

            var countries = await query
                .Select(c => new CountryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Active = c.Active,
                })
                .ToListAsync();

            return Ok(countries);
        }

        [HttpPost("add")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> AddNew([FromBody] AddCountryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Country name is required." });
            }

            var normalizedName = request.Name.Trim();
            var alreadyExists = await _dbContext.Countries.AnyAsync(c => c.Name == normalizedName);
            if (alreadyExists)
            {
                return Conflict(new { message = "Country already exists." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var modifiedByUserId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 1;

            var country = new Country
            {
                Name = normalizedName,
                Active = request.Active,
                ModifiedDate = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId,
            };

            _dbContext.Countries.Add(country);
            await _dbContext.SaveChangesAsync();

            return Ok(new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Active = country.Active,
            });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCountryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Country name is required." });
            }

            var country = await _dbContext.Countries.FindAsync(id);
            if (country == null)
            {
                return NotFound(new { message = "Country not found." });
            }

            var normalizedName = request.Name.Trim();
            var duplicateExists = await _dbContext.Countries.AnyAsync(c => c.Name == normalizedName && c.Id != id);
            if (duplicateExists)
            {
                return Conflict(new { message = "Another country with this name already exists." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var modifiedByUserId = int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 1;

            country.Name = normalizedName;
            country.Active = request.Active;
            country.ModifiedDate = DateTime.UtcNow;
            country.ModifiedByUserId = modifiedByUserId;

            await _dbContext.SaveChangesAsync();

            return Ok(new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Active = country.Active,
            });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _dbContext.Countries.FindAsync(id);
            if (country == null)
            {
                return NotFound(new { message = "Country not found." });
            }

            _dbContext.Countries.Remove(country);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        public class AddCountryRequest
        {
            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; } = true;
        }

        public class UpdateCountryRequest
        {
            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; }
        }

        public class CountryDto
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;

            public bool Active { get; set; }
        }
    }
}
