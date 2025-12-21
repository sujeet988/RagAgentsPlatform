using RagAgents.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class RagService : IRagService
    {
        private readonly IAzureOpenAIService _openAI;
        private readonly IAzureSearchService _search;
        private readonly IConversationStore _store;
        public RagService(IAzureOpenAIService openAI,IAzureSearchService search,IConversationStore store)
        {
            _openAI = openAI;
            _search = search;
            _store = store;

        }
        public async Task<string> AskAsync(string question)
        {
            var embedding = await _openAI.CreateEmbeddingAsync(question);
            var chunks = await _search.VectorSearchAsync(embedding);

            var context = string.Join("\n", chunks);

            var prompt = $"""
            Answer using ONLY the context below.
            If information is missing, say "Information not available".

            Context:
            {context}

            Question:
            {question}
            """;

            return await _openAI.GenerateAnswerAsync(prompt);
        }

        public async Task AskWithHistoryAsync(string question, string conversationId, string userId, Func<string, Task> onToken)
        {
            throw new NotImplementedException();
        }
    }
}
