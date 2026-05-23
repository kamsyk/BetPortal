using BetPortalWebApi.Contracts.Chat;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : BpBaseController
    {
        private readonly IChatbotService _chatbotService;

        public ChatController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatAskRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            var response = await _chatbotService.AskAsync(request.Message, cancellationToken);
            return Ok(response);
        }

        [HttpPost("reindex")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> Reindex(CancellationToken cancellationToken)
        {
            var response = await _chatbotService.ReindexAsync(cancellationToken);
            if (!response.Success)
            {
                return StatusCode(500, response);
            }

            return Ok(response);
        }
    }
}
