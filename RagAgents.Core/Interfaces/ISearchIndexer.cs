using Azure.Search.Documents;
using Azure.Search.Documents.Models;
namespace RagAgents.Core.Interfaces
{
    public interface ISearchIndexer
    {
        Task IndexAsync<T>(T document);
        Task<List<string>> VectorSearchAsync(float[] embedding);
        Task CreateIndexIfNotExistsAsync();
    }
}
