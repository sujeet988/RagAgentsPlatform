namespace RagAgents.Core.Models;

public sealed record DocumentIngestRequest(
    Stream Content,
    string FileName,
    string? SourceUri = null,
    string? DocumentId = null,
    string? DocumentVersion = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record DocumentIngestResult(
    string DocumentId,
    string DocumentVersion,
    string FileName,
    int ChunkCount,
    DateTimeOffset IngestedAt);

public sealed record DocumentChunk(
    string Id,
    string DocumentId,
    string DocumentVersion,
    string SourceFileName,
    string? SourceUri,
    int ChunkIndex,
    string Content,
    float[] Embedding,
    DateTimeOffset IngestedAt,
    string ContentHash);

public sealed class IngestionOptions
{
    public int ChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 100;

    public int EmbeddingBatchSize { get; set; } = 16;
}