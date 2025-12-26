using Microsoft.Azure.Cosmos;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class ConversationStore : IConversationStore
    {
        private readonly Microsoft.Azure.Cosmos.Container _container;
        public ConversationStore(CosmosClient client)
        {
            _container = client
            .GetDatabase("rag-chat-db")
            .GetContainer("messages");
        }
        public async Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId, string userId,int maxMessages)
        {
            var query = new QueryDefinition(
           "SELECT * FROM c WHERE c.userId = @uid AND c.conversationId = @cid ORDER BY c.timestamp DESC")
           .WithParameter("@uid", userId)
           .WithParameter("@cid", conversationId);

            var iterator = _container.GetItemQueryIterator<ChatMessageModel>(query);
            var results = new List<ChatMessageModel>();

            while (iterator.HasMoreResults && results.Count < maxMessages)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page);
            }

            return results;
        }

        public async Task SaveMessageAsync(ChatMessageModel message)
        {
            await _container.UpsertItemAsync(message,
           new PartitionKey(message.UserId));
        }
    }
}
