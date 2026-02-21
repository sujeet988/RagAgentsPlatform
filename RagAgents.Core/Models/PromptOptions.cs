using System.Collections.Generic;

namespace RagAgents.Core.Models
{
    public class PromptOptions
    {
        public string SystemPrompt { get; set; } = "You are an enterprise assistant. Answer only using context provided.";
        
        public RagPromptTemplates RagPrompts { get; set; } = new();
        
        public Dictionary<string, string> CustomPrompts { get; set; } = new();
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
