
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
        public async Task<IReadOnlyList<ChatMessageModel>> GetHistoryAsync(string conversationId, int maxMessages)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.conversationId = @cid ORDER BY c.timestamp DESC")
                .WithParameter("@cid", conversationId);

                var iterator = _container.GetItemQueryIterator<ChatMessageModel>(
                    query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(conversationId),
                        MaxItemCount = maxMessages
                    });

                var results = new List<ChatMessageModel>();

                while (iterator.HasMoreResults && results.Count < maxMessages)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }

                // Return oldest → newest
                return results
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }
            catch(Exception ex)
            {
                 throw;
            }
        }

        public async Task SaveMessageAsync(ChatMessageModel message)
        {
            await _container.CreateItemAsync(
             message,
             new PartitionKey(message.ConversationId));
        }
    }
}
