using RagAgents.Api.Services;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System.Security.Claims;

namespace RagAgents.Api.Services
{
    public class ChatService : IChatService
    {
        private readonly IRagService _ragService;
        private readonly IConversationStoreInMemory _conversationStore;

        public ChatService(IRagService ragService, IConversationStoreInMemory conversationStore)
        {
            _ragService = ragService;
            _conversationStore = conversationStore;
        }

        public async Task<ChatMessageModel?> AskAsync(QuestionRequest request, ClaimsPrincipal user)
        {
            var userId = user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var history = await _conversationStore.GetHistoryAsync(request.ConversationId, userId, 10);

            if (string.IsNullOrWhiteSpace(request.Question))
                return null;

            var answer = await _ragService.AskAsync(request.Question);
            var chatMessageModel = new ChatMessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Content = answer,
                Role = "assistant"
            };

            return chatMessageModel;
        }

        public async Task<ChatMessageModel?> AskWithoutAuthAsync(QuestionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return null;

            var answer = await _ragService.AskAsync(request.Question);
            var chatMessageModel = new ChatMessageModel
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Content = answer,
                Role = "assistant"
            };

            return chatMessageModel;
        }

        public async Task<ChatMessageModel?> AskWithHistoryAsync(QuestionRequest request, ClaimsPrincipal user)
        {
            var userId = user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            if (string.IsNullOrWhiteSpace(request.Question))
                return null;

            var answer = await _ragService.AskWithHistoryNoStreamAsync(request.Question, request.ConversationId, userId);
            return answer;
        }

        public async Task<IEnumerable<ChatMessageModel>> GetHistoryAsync(string conversationId, ClaimsPrincipal user)
        {
            var userId = user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(userId))
                return Enumerable.Empty<ChatMessageModel>();

            var history = await _conversationStore.GetHistoryAsync(conversationId, userId, maxMessages: 50);
            return history;
        }
    }
}
