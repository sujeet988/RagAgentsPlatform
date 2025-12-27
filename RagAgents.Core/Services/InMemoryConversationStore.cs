using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class InMemoryConversationStore : IConversationStoreInMemory
    {
        private static readonly List<ChatMessageModel> _messages = new();
        public Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId, string userId, int maxMessages=50)
        {
            var history = _messages
           .Where(m =>
               m.ConversationId == conversationId &&
               m.UserId == userId)
           .OrderByDescending(m => m.Timestamp)
           .Take(maxMessages)
           .OrderBy(m => m.Timestamp)
           .ToList();

            return Task.FromResult<IReadOnlyList<ChatMessageModel>>(history);
        }

        public Task SaveMessageAsync(ChatMessageModel message)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
