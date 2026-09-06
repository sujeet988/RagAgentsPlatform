using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class RagService : IRagService
    {
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;
        private readonly ISearchIndexer _search;
        private readonly IConversationStoreInMemory _conversationStore;
        public RagService(IOpenAIEmbeddingService openAIEmbeddingService,ISearchIndexer search, IConversationStoreInMemory conversationStore)
        {
            _openAIEmbeddingService = openAIEmbeddingService;
            _search = search;
            _conversationStore = conversationStore;

        }
        public async Task<string> AskAsync(string question)
        {
            var embedding = await _openAIEmbeddingService.CreateEmbeddingAsync(question);
            var chunks = await _search.VectorSearchAsync(embedding);

            var context = string.Join("\n", chunks);

            var prompt = PromptTemplates.AnswerWithContext(context, question);

            return await _openAIEmbeddingService.GenerateAnswerAsync(prompt);
        }

        public async Task<ChatMessageModel> AskWithHistoryNoStreamAsync(string question, string conversationId, string userId)
        {
            // 1️⃣ Save user message
            await _conversationStore.SaveMessageAsync(new ChatMessageModel
            {
                ConversationId = conversationId,
                UserId = userId,
                Role = "user",
                Content = question
            });

            // Load recent history
            var history = await _conversationStore.GetHistoryAsync(
                conversationId, userId, 50);

            var historyText = string.Join("\n",
                history.Select(m => $"{m.Role}: {m.Content}"));

            // 3️RAG retrieval
            var embedding = await _openAIEmbeddingService.CreateEmbeddingAsync(question);
            var chunks = await _search.VectorSearchAsync(embedding);
            var context = string.Join("\n", chunks);

            // 4️Prompt
                var prompt = PromptTemplates.AnswerWithHistory(historyText, context, question);

            // 5️⃣ Call LLM (NON-streaming)
            var answer = await _openAIEmbeddingService.GenerateAnswerAsync(prompt);

            // 6️⃣ Save assistant message
            var assistantMessage = new ChatMessageModel
            {
                ConversationId = conversationId,
                UserId = userId,
                Role = "assistant",
                Content = answer
            };

            await _conversationStore.SaveMessageAsync(assistantMessage);

            return assistantMessage;
        }
        public async Task AskWithHistoryAsync(string question, string conversationId, string userId, Func<string, Task> onToken)
        {
            throw new NotImplementedException();
        }

    }
}
