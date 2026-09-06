using Microsoft.Extensions.Logging;
using RagAgents.Core.Agents;
using RagAgents.Core.Helpers;
using RagAgents.Core.Interfaces;
using System.Text.Json;
using System.Linq;

namespace RagAgents.Core.Agents
{
    // A lightweight agent that asks the LLM to produce a JSON plan and executes tools.
    public class AgentService : IAgent
    {
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;
        private readonly IEnumerable<ITool> _tools;
        private readonly ILogger<AgentService> _logger;
        private readonly Dictionary<string, string> _aliases;
        private readonly HashSet<string> _allowedTools;
        private readonly int _maxSteps = 10;

        public AgentService(IOpenAIEmbeddingService openAIEmbeddingService, IEnumerable<ITool> tools, ILogger<AgentService> logger)
        {
            _openAIEmbeddingService = openAIEmbeddingService;
            _tools = tools;
            _logger = logger;

            // Build allowed tools set from registered tools
            _allowedTools = new HashSet<string>(_tools.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

            // Common alias mapping to canonical tool names
            _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "searchtool", "Search" }, { "search", "Search" }, { "find", "Search" },
                { "pdfingest", "PdfIngest" }, { "pdf_ingest", "PdfIngest" }, { "ingestpdf", "PdfIngest" }, { "pdf", "PdfIngest" }
            };
        }

        public async Task<string> RunAsync(string goal, string conversationId = null)
        {
            // 1) Ask LLM for a simple JSON plan. Include allowed tools in the prompt so LLM uses exact names.
            var allowedList = string.Join(", ", _allowedTools);
            var planPrompt =
                "Create a JSON array of steps to achieve the goal: " + goal + Environment.NewLine +
                "Each step MUST be an object with two properties: 'tool' and 'input'." + Environment.NewLine +
                "Only use the following tools (case sensitive names): " + allowedList + "." + Environment.NewLine +
                "Return ONLY valid JSON array, for example: [{\"tool\":\"Search\",\"input\":\"...\"}]";

            _logger.LogInformation("Agent plan prompt: {prompt}", planPrompt);

            var planJson = await _openAIEmbeddingService.GenerateAnswerAsync(planPrompt);

            AgentStep[] steps;
            try
            {
                steps = JsonSerializer.Deserialize<AgentStep[]>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Array.Empty<AgentStep>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse plan JSON from LLM. Falling back to single Search step.");
                steps = new[] { new AgentStep("Search", goal) };
            }

            if (steps.Length > _maxSteps)
            {
                _logger.LogWarning("Plan contained {count} steps, trimming to {max}", steps.Length, _maxSteps);
                steps = steps.Take(_maxSteps).ToArray();
            }

            string lastResult = string.Empty;

            foreach (var rawStep in steps)
            {
                if (string.IsNullOrWhiteSpace(rawStep?.Tool))
                {
                    _logger.LogWarning("Skipping step with empty tool");
                    continue;
                }

                // Normalize tool name via alias or direct match
                var toolKey = rawStep.Tool.Trim();
                var canonical = toolKey;
                if (_aliases.TryGetValue(toolKey, out var mapped))
                    canonical = mapped;
                else if (_allowedTools.Contains(toolKey, StringComparer.OrdinalIgnoreCase))
                {
                    // find canonical casing from registered tools
                    var found = _tools.FirstOrDefault(t => t.Name.Equals(toolKey, StringComparison.OrdinalIgnoreCase));
                    if (found != null) canonical = found.Name;
                }

                if (!_allowedTools.Contains(canonical, StringComparer.OrdinalIgnoreCase))
                {
                    lastResult = $"Unknown or disallowed tool: {rawStep.Tool}";
                    _logger.LogWarning("{msg}", lastResult);
                    continue;
                }

                var tool = _tools.SingleOrDefault(t => t.Name.Equals(canonical, StringComparison.OrdinalIgnoreCase));
                if (tool == null)
                {
                    lastResult = $"Tool not found after normalization: {canonical}";
                    _logger.LogWarning("{msg}", lastResult);
                    continue;
                }

                _logger.LogInformation("Executing tool {tool} with input: {input}", tool.Name, rawStep.Input);
                try
                {
                    lastResult = await tool.RunAsync(rawStep.Input ?? string.Empty);
                    _logger.LogInformation("Tool {tool} completed. Result length: {len}", tool.Name, lastResult?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    lastResult = $"Tool {tool.Name} failed: {ex.Message}";
                    _logger.LogError(ex, "Tool {tool} execution failed", tool.Name);
                }
            }

            return lastResult;
        }
    }
}
