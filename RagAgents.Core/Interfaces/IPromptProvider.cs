using RagAgents.Core.Models;

namespace RagAgents.Core.Interfaces
{
    public interface IPromptProvider
    {
        string GetSystemPrompt(string? version = null);
        string GetSimpleRagPrompt(string context, string question);
        string GetRagWithHistoryPrompt(string history, string context, string question);
        string GetCustomPrompt(string promptName, Dictionary<string, string> parameters);
        string GetCurrentVersion();
        PromptVersion? GetVersionedPrompts(string version);
    }
}
