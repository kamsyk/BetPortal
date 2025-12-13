using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SportController : BpBaseController
    {
        [HttpGet("list")]
        [Authorize]
        public IActionResult GetSportsList()
        {
            return Ok();
        }
    }
}
