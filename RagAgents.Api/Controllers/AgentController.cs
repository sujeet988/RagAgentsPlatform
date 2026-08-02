using Microsoft.AspNetCore.Mvc;
using RagAgents.Core.Agents;

namespace RagAgents.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly IAgent _agent;

        public AgentController(IAgent agent)
        {
            _agent = agent;
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run([FromBody] RunRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Goal))
                return BadRequest("Goal is required");

            var result = await _agent.RunAsync(request.Goal);
            return Ok(new { result });
        }
    }

    public class RunRequest
    {
        public string Goal { get; set; }
    }
}
