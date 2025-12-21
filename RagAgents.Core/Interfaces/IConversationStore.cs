using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IConversationStore
    {
        Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId,int maxMessages);

        Task SaveMessageAsync(ChatMessageModel message);
    }

}
