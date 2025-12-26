using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenAI.RealtimeConversation;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;

namespace RagAgents.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RagUser")]
    public class ChatController : ControllerBase
    {
        private readonly IRagService _ragService;
        public ChatController(IRagService ragService)
        {
            _ragService = ragService;
        }

        // POST: api/rag/ask
        [HttpPost]
        [Route("ask")]
        public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is missing");

            var answer = await _ragService.AskAsync(request.Question);
            ChatMessageModel chatMessageModel = new ChatMessageModel();
            chatMessageModel.Id = Guid.NewGuid().ToString();
            chatMessageModel.Timestamp = DateTime.UtcNow;
            chatMessageModel.Content = answer;
            chatMessageModel.Role = "assistant";
            return Ok(new { chatMessageModel });
        }

        [HttpGet]
        [Route("ping")]
        [AllowAnonymous]
        public  IActionResult ping()
        {
            return Ok("pong");
        }

    }
}
