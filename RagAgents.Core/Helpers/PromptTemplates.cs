namespace RagAgents.Core.Helpers
{
    /// <summary>
    /// Centralized prompt templates for RAG operations. Keep templates here so they can be
    /// reused and updated in a single place.
    /// </summary>
    public static class PromptTemplates
    {
        public static string AnswerWithContext(string context, string question)
        {
            return $"""
Answer using ONLY the context below.
If information is missing, say \"Information not available\".

Context:
{context}

Question:
{question}
""";
        }

        public static string AnswerWithHistory(string historyText, string context, string question)
        {
            return $"""
You are a helpful AI assistant.

Conversation History:
{historyText}

Use ONLY the context below.
If information is missing, say \"Information not available\".

Context:
{context}

Question:
{question}
""";
        }
    }
}
