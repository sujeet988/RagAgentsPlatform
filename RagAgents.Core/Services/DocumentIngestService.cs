using RagAgents.Core.Helpers;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace RagAgents.Core.Services
{
    public class DocumentIngestService : IDocumentIngestService
    {
        private readonly IDocumentTextExtractor _textExtractor;
        private readonly IOpenAIEmbeddingService _openAIEmbeddingService;
        private readonly ISearchIndexer _search;
        private readonly ILogger<DocumentIngestService> _logger;
        private readonly IngestionOptions _options;

        public DocumentIngestService(
            IDocumentTextExtractor textExtractor,
            IOpenAIEmbeddingService openAIEmbeddingService,
            ISearchIndexer search,
            IOptions<IngestionOptions> options,
            ILogger<DocumentIngestService> logger)
        {
            _textExtractor = textExtractor;
            _openAIEmbeddingService = openAIEmbeddingService;
            _search = search;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<DocumentIngestResult> IngestAsync(DocumentIngestRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Content);

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                throw new ArgumentException("A source file name is required for ingestion.", nameof(request));
            }

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["FileName"] = request.FileName,
                ["SourceUri"] = request.SourceUri ?? string.Empty
            });

            _logger.LogInformation("Starting document ingestion.");

            var text = await _textExtractor.ExtractTextAsync(request.Content, cancellationToken);
            var documentId = ResolveDocumentId(request.FileName, request.DocumentId);
            var ingestedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Document ingestion skipped because no text was extracted.");

                return new DocumentIngestResult(
                    documentId,
                    request.DocumentVersion ?? "empty",
                    request.FileName,
                    0,
                    ingestedAt);
            }

            var contentHash = ComputeSha256(text);
            var documentVersion = string.IsNullOrWhiteSpace(request.DocumentVersion)
                ? contentHash
                : request.DocumentVersion;

            var chunks = ChunkingHelper
                .SplitTextByOverLap(text, _options.ChunkSize, _options.ChunkOverlap)
                .Where(chunk => !string.IsNullOrWhiteSpace(chunk))
                .ToArray();

            if (chunks.Length == 0)
            {
                _logger.LogWarning("Document ingestion skipped because chunking produced no chunks.");
                return new DocumentIngestResult(documentId, documentVersion, request.FileName, 0, ingestedAt);
            }

            await _search.CreateIndexIfNotExistsAsync(cancellationToken);

            var indexedCount = 0;
            foreach (var batch in chunks.Select((content, index) => new { content, index }).Chunk(_options.EmbeddingBatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchTexts = batch.Select(item => item.content).ToArray();
                var embeddings = await _openAIEmbeddingService.CreateEmbeddingsBatchAsync(batchTexts, cancellationToken);

                if (embeddings.Count != batchTexts.Length)
                {
                    throw new InvalidOperationException($"Embedding batch returned {embeddings.Count} vectors for {batchTexts.Length} chunks.");
                }

                var documents = batch
                    .Select((item, batchIndex) => new DocumentChunk(
                        Id: CreateChunkId(documentId, documentVersion, item.index),
                        DocumentId: documentId,
                        DocumentVersion: documentVersion,
                        SourceFileName: request.FileName,
                        SourceUri: request.SourceUri,
                        ChunkIndex: item.index,
                        Content: item.content,
                        Embedding: embeddings[batchIndex],
                        IngestedAt: ingestedAt,
                        ContentHash: contentHash))
                    .ToArray();

                await _search.IndexChunksAsync(documents, cancellationToken);
                indexedCount += documents.Length;
            }

            _logger.LogInformation(
                "Completed document ingestion. DocumentId: {DocumentId}; Version: {DocumentVersion}; Chunks: {ChunkCount}.",
                documentId,
                documentVersion,
                indexedCount);

            return new DocumentIngestResult(documentId, documentVersion, request.FileName, indexedCount, ingestedAt);
        }

        private static string ResolveDocumentId(string fileName, string? documentId)
        {
            return string.IsNullOrWhiteSpace(documentId)
                ? ComputeSha256(fileName.Trim().ToLowerInvariant())
                : documentId;
        }

        private static string CreateChunkId(string documentId, string documentVersion, int chunkIndex)
        {
            return ComputeSha256($"{documentId}:{documentVersion}:{chunkIndex}");
        }

        private static string ComputeSha256(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
       
    }
    
}
