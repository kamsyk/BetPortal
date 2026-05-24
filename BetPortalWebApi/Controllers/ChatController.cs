using BetPortalWebApi.Contracts.Chat;
using BetPortalWebApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BetPortalWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : BpBaseController
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("ask")]
        public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest("Message cannot be empty");
                }

                var response = await _chatService.GetChatResponseAsync(request.Message, request.ConversationHistory);

                return Ok(new ChatResponse
                {
                    Content = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
