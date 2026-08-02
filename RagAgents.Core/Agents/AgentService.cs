using RagAgents.Core.Agents;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Helpers;
using System.Text.Json;

namespace RagAgents.Core.Agents
{
    // A lightweight agent that asks the LLM to produce a JSON plan and executes tools.
    public class AgentService : IAgent
    {
        private readonly IAzureOpenAIService _openAI;
        private readonly IEnumerable<ITool> _tools;

        public AgentService(IAzureOpenAIService openAI, IEnumerable<ITool> tools)
        {
            _openAI = openAI;
            _tools = tools;
        }

        public async Task<string> RunAsync(string goal, string conversationId = null)
        {
            // 1) Ask LLM for a simple JSON plan
            var planPrompt = $"Create a JSON array of steps to achieve the goal: {goal}. " +
                             "Each step must be an object with 'tool' and 'input' properties. " +
                             "Return ONLY valid JSON.";

            var planJson = await _openAI.GenerateAnswerAsync(planPrompt);

            AgentStep[] steps;
            try
            {
                steps = JsonSerializer.Deserialize<AgentStep[]>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Array.Empty<AgentStep>();
            }
            catch
            {
                // If LLM didn't return JSON, fallback to a single search step
                steps = new[] { new AgentStep("Search", goal) };
            }

            // 2) Execute steps sequentially
            string lastResult = string.Empty;
            foreach (var step in steps)
            {
                var tool = _tools.SingleOrDefault(t => t.Name.Equals(step.Tool, StringComparison.OrdinalIgnoreCase));
                if (tool == null)
                {
                    lastResult = $"Unknown tool: {step.Tool}";
                    continue;
                }

                try
                {
                    lastResult = await tool.RunAsync(step.Input);
                }
                catch (Exception ex)
                {
                    lastResult = $"Tool {step.Tool} failed: {ex.Message}";
                }
            }

            return lastResult;
        }
    }
}
