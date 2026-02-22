using System.Collections.Generic;

namespace RagAgents.Core.Models
{
    public class PromptOptions
    {
        public string SystemPrompt { get; set; } = "You are an enterprise assistant. Answer only using context provided.";
        
        public RagPromptTemplates RagPrompts { get; set; } = new();
        
        public Dictionary<string, string> CustomPrompts { get; set; } = new();
        
        // Prompt Versioning
        public string CurrentVersion { get; set; } = "v1.0";
        public bool EnableVersionTracking { get; set; } = true;
        public Dictionary<string, PromptVersion> Versions { get; set; } = new();
    }
    
    public class PromptVersion
    {
        public string Version { get; set; } = default!;
        public string SystemPrompt { get; set; } = default!;
        public RagPromptTemplates RagPrompts { get; set; } = new();
        public Dictionary<string, string> CustomPrompts { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = "system";
        public bool IsActive { get; set; } = true;
        public string Description { get; set; } = string.Empty;
    }

    public class RagPromptTemplates
    {
        public string SimpleRag { get; set; } = """
            Answer using ONLY the context below.
            If information is missing, say "Information not available".

            Context:
            {context}

            Question:
            {question}
            """;

        public string RagWithHistory { get; set; } = """
            You are a helpful AI assistant.

            Conversation History:
            {history}

            Use ONLY the context below.
            If information is missing, say "Information not available".

            Context:
            {context}

            Question:
            {question}
            """;

        public string RagWithCitations { get; set; } = """
            You are a helpful AI assistant. Answer using ONLY the context below.
            Include citations [1], [2], etc. when referencing context.
            If information is missing, say "Information not available".

            Context:
            {context}

            Question:
            {question}
            """;
    }
}
