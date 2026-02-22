using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System.Diagnostics;

namespace RagAgents.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VersionManagementController : ControllerBase
    {
        private readonly IVersionTrackingService _versionTrackingService;
        private readonly IPromptProvider _promptProvider;
        private readonly IAzureOpenAIService _openAIService;
        private readonly ILogger<VersionManagementController> _logger;

        public VersionManagementController(
            IVersionTrackingService versionTrackingService,
            IPromptProvider promptProvider,
            IAzureOpenAIService openAIService,
            ILogger<VersionManagementController> logger)
        {
            _versionTrackingService = versionTrackingService;
            _promptProvider = promptProvider;
            _openAIService = openAIService;
            _logger = logger;
        }

        /// <summary>
        /// Get current model versions
        /// </summary>
        [HttpGet("models/current")]
        public IActionResult GetCurrentModelVersions()
        {
            return Ok(new
            {
                chatModel = _openAIService.GetChatModelVersion(),
                embeddingModel = _openAIService.GetEmbeddingModelVersion()
            });
        }

        /// <summary>
        /// Get current prompt version
        /// </summary>
        [HttpGet("prompts/current")]
        public IActionResult GetCurrentPromptVersion()
        {
            return Ok(new
            {
                version = _promptProvider.GetCurrentVersion()
            });
        }

        /// <summary>
        /// Get specific prompt version details
        /// </summary>
        [HttpGet("prompts/{version}")]
        public IActionResult GetPromptVersion(string version)
        {
            var promptVersion = _promptProvider.GetVersionedPrompts(version);
            if (promptVersion == null)
            {
                return NotFound(new { error = $"Prompt version '{version}' not found" });
            }

            return Ok(promptVersion);
        }

        /// <summary>
        /// Get model usage statistics
        /// </summary>
        [HttpGet("models/{modelVersion}/stats")]
        public async Task<IActionResult> GetModelStats(
            string modelVersion,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _versionTrackingService.GetModelUsageStatsAsync(modelVersion, start, end);
            return Ok(stats);
        }

        /// <summary>
        /// Get prompt usage statistics
        /// </summary>
        [HttpGet("prompts/{promptVersion}/stats")]
        public async Task<IActionResult> GetPromptStats(
            string promptVersion,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var stats = await _versionTrackingService.GetPromptUsageStatsAsync(promptVersion, start, end);
            return Ok(stats);
        }

        /// <summary>
        /// Switch model version (requires Azure OpenAI deployment update)
        /// </summary>
        [HttpPost("models/switch")]
        public async Task<IActionResult> SwitchModelVersion([FromBody] SwitchModelRequest request)
        {
            var success = await _versionTrackingService.SwitchModelVersionAsync(
                request.DeploymentName,
                request.NewVersion);

            if (success)
            {
                return Ok(new { message = "Model version switch initiated" });
            }

            return BadRequest(new { error = "Model version switch failed. Check logs for details." });
        }

        /// <summary>
        /// Switch prompt version (requires app restart)
        /// </summary>
        [HttpPost("prompts/switch")]
        public async Task<IActionResult> SwitchPromptVersion([FromBody] SwitchPromptRequest request)
        {
            var success = await _versionTrackingService.SwitchPromptVersionAsync(request.NewVersion);

            if (success)
            {
                return Ok(new
                {
                    message = "Prompt version switch recorded. Restart application to apply changes.",
                    newVersion = request.NewVersion
                });
            }

            return BadRequest(new { error = "Prompt version not found in configuration" });
        }

        /// <summary>
        /// Track a test prompt execution (for A/B testing)
        /// </summary>
        [HttpPost("prompts/track-test")]
        public async Task<IActionResult> TrackPromptTest([FromBody] PromptTestRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // This is a simplified example - in practice, you'd execute the prompt
                stopwatch.Stop();

                await _versionTrackingService.TrackPromptUsageAsync(new PromptUsageRecord
                {
                    PromptVersion = request.PromptVersion,
                    PromptType = request.PromptType,
                    Question = request.Question,
                    ModelVersion = _openAIService.GetChatModelVersion(),
                    ResponseLength = request.ResponseLength,
                    ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    IsSuccess = true,
                    Timestamp = DateTime.UtcNow,
                    UserId = User.Identity?.Name ?? "anonymous"
                });

                return Ok(new { message = "Prompt test tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking prompt test");
                return StatusCode(500, new { error = "Failed to track prompt test" });
            }
        }
    }

    public class SwitchModelRequest
    {
        public string DeploymentName { get; set; } = default!;
        public string NewVersion { get; set; } = default!;
    }

    public class SwitchPromptRequest
    {
        public string NewVersion { get; set; } = default!;
    }

    public class PromptTestRequest
    {
        public string PromptVersion { get; set; } = default!;
        public string PromptType { get; set; } = default!;
        public string Question { get; set; } = default!;
        public int ResponseLength { get; set; }
    }
}
