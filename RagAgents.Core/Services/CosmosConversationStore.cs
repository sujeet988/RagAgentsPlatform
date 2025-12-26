
using Microsoft.Azure.Cosmos;
using Microsoft.VisualBasic;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;


namespace RagAgents.Core.Services
{
    public class CosmosConversationStore  : IConversationStore
    {
        private readonly Container _container;
        public CosmosConversationStore(CosmosClient client)
        {
          _container = client
         .GetDatabase("rag-chat-db")
         .GetContainer("conversations");

        }
        public async Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId, string userId, int maxMessages)
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
            await _container.UpsertItemAsync(
           message,
           new PartitionKey(message.UserId));
        }
    }
}
