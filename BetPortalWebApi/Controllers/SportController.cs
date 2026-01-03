using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SportController : BpBaseController
    {
        [HttpGet("list")]
        public IActionResult GetSportsList()
        {
            return Ok();
        }

        [HttpPost("add")]
        [Authorize(Policy = "AdminAccess")]
        public IActionResult AddNew()
        {
            return Ok();
        }
    }
}
