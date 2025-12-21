using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class ConversationStore : IConversationStore
    {
        public ConversationStore()
        {
            
        }
        public async Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId, int maxMessages)
        {
            throw new NotImplementedException();
        }

        public async Task SaveMessageAsync(ChatMessageModel message)
        {
            throw new NotImplementedException();
        }
    }
}
