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
    //[Authorize(Policy = "RagAdmin")]
    public class ChatController : ControllerBase
    {
        private readonly RagAgents.Api.Services.IChatService _chatService;

        public ChatController(RagAgents.Api.Services.IChatService chatService)
        {
            _chatService = chatService;
        }

        // POST: api/rag/ask
        [HttpPost]
        [Route("ask")]
        public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
        {
            var result = await _chatService.AskAsync(request, User);
            if (result == null)
                return BadRequest("Unable to process request or user missing");
            return Ok(new { chatMessageModel = result });

        }

        // POST: api/rag/ask
        [HttpPost]
        [Route("askwithnoauth")]
        public async Task<IActionResult> askwithnoauth([FromBody] QuestionRequest request)
        {

            var result = await _chatService.AskWithoutAuthAsync(request);
            if (result == null)
                return BadRequest("Question is missing");
            return Ok(new { chatMessageModel = result });

        }

        // POST: api/rag/ask
        [HttpPost]
        [Route("askwithhistory")]
        public async Task<IActionResult> Askwithhistory([FromBody] QuestionRequest request)
        {
            var result = await _chatService.AskWithHistoryAsync(request, User);
            if (result == null)
                return BadRequest("Unable to process request or user missing");
            return Ok(result);
        }

        [HttpGet("history/{conversationId}")]
        public async Task<IActionResult> History(string conversationId)
        {
            var history = await _chatService.GetHistoryAsync(conversationId, User);
            if (history == null)
                return Unauthorized();
            return Ok(history);
        }

        [HttpGet]
        [Route("ping")]
        public IActionResult ping()
        {
            return Ok("pong");
        }


    }
}
