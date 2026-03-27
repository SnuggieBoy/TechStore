using Microsoft.AspNetCore.Mvc;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IAIChatService _chatService;

        public ChatController(IAIChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            var result = await _chatService.AskAsync(question);
            return Ok(result);
        }
    }
}
