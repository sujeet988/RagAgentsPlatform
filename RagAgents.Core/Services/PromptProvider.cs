using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System.Text.RegularExpressions;

namespace RagAgents.Core.Services
{
    public class PromptProvider : IPromptProvider
    {
        private readonly PromptOptions _options;
        private readonly string _currentVersion;

        public PromptProvider(IOptions<PromptOptions> options)
        {
            _options = options.Value;
            _currentVersion = _options.CurrentVersion;
        }
        
        public string GetCurrentVersion() => _currentVersion;
        
        public PromptVersion? GetVersionedPrompts(string version)
        {
            return _options.Versions.TryGetValue(version, out var promptVersion) 
                ? promptVersion 
                : null;
        }

        public string GetSystemPrompt(string? version = null)
        {
            if (!string.IsNullOrEmpty(version) && _options.Versions.TryGetValue(version, out var promptVersion))
            {
                return promptVersion.SystemPrompt;
            }
            return _options.SystemPrompt;
        }

        public string GetSimpleRagPrompt(string context, string question)
        {
            return _options.RagPrompts.SimpleRag
                .Replace("{context}", context)
                .Replace("{question}", question);
        }

        public string GetRagWithHistoryPrompt(string history, string context, string question)
        {
            return _options.RagPrompts.RagWithHistory
                .Replace("{history}", history)
                .Replace("{context}", context)
                .Replace("{question}", question);
        }

        public string GetCustomPrompt(string promptName, Dictionary<string, string> parameters)
        {
            if (!_options.CustomPrompts.TryGetValue(promptName, out var template))
            {
                throw new ArgumentException($"Prompt '{promptName}' not found in configuration");
            }

            var result = template;
            foreach (var param in parameters)
            {
                result = result.Replace($"{{{param.Key}}}", param.Value);
            }

            return result;
        }
    }
}
