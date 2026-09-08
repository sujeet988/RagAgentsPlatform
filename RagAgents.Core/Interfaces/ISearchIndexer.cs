using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using RagAgents.Core.Models;
namespace RagAgents.Core.Interfaces
{
    public interface ISearchIndexer
    {
        Task IndexAsync<T>(T document);
        Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
        Task<List<string>> VectorSearchAsync(float[] embedding);
        Task CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default);
    }
}
