using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using OpenAI.RealtimeConversation;
using RagAgents.Api.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System.Security.Claims;

namespace RagAgents.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RagAdmin")]
    public class ChatController : ControllerBase
    {
        private readonly IRagService _ragService;
        private readonly IConversationStore _conversationStore;
        public ChatController(IRagService ragService, IConversationStore conversationStore)
        {
            _ragService = ragService;
            _conversationStore = conversationStore;
        }

        // POST: api/rag/ask
        [HttpPost]
        [Route("ask")]
        public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
        {
            var userId = User.GetUserId();          // GUID
            var email = User.GetUserEmail();       // user@company.com

            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("User ID is missing");

            // get history of data 
            var history = await _conversationStore.GetHistoryAsync(
                                request.ConversationId, userId, 10);

            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest("Question is missing");

            //var answer = await _ragService.AskAsync(request.Question);
            //ChatMessageModel chatMessageModel = new ChatMessageModel();
            //chatMessageModel.Id = Guid.NewGuid().ToString();
            //chatMessageModel.Timestamp = DateTime.UtcNow;
            //chatMessageModel.Content = answer;
            //chatMessageModel.Role = "assistant";
            //return Ok(new { chatMessageModel });

            // Ask with History
            var answer = await _ragService.AskWithHistoryAsync(request.Question,
                                request.ConversationId,userId);

            await _conversationStore.SaveMessageAsync(answer);

            return Ok(answer);

        }

        [HttpGet]
        [Route("ping")]
        public  IActionResult ping()
        {
            return Ok("pong");
        }

    }
}
