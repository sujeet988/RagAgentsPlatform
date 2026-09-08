using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;

namespace RagAgents.Core.Services
{
    public class AzureSearchSearchIndexerService : ISearchIndexer
    {
        private readonly SearchClient _client;
        private readonly SearchIndexClient _indexClient;
        private readonly ILogger<AzureSearchSearchIndexerService> _logger;
        private readonly string _indexName;
        private readonly int _vectorDimensions;

        public AzureSearchSearchIndexerService(
            IOptions<AzureSearchAIOptions> options,
            ILogger<AzureSearchSearchIndexerService> logger)
        {
            var cfg = options.Value;
            if (string.IsNullOrWhiteSpace(cfg.Endpoint))
            {
                throw new InvalidOperationException("AzureSearch:Endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(cfg.Key))
            {
                throw new InvalidOperationException("AzureSearch:Key is required.");
            }

            if (string.IsNullOrWhiteSpace(cfg.IndexName))
            {
                throw new InvalidOperationException("AzureSearch:IndexName is required.");
            }

            _logger = logger;
            _client = new SearchClient(
                new Uri(cfg.Endpoint),
                cfg.IndexName,
                new AzureKeyCredential(cfg.Key));
            _indexName = cfg.IndexName;
            _vectorDimensions = cfg.VectorDimensions;
            _indexClient  = new SearchIndexClient(
                new Uri(cfg.Endpoint),
                new AzureKeyCredential(cfg.Key));
        }

        public async Task IndexAsync<T>(T document)
        {
            await _client.UploadDocumentsAsync(new[] { document });
        }

        public async Task IndexChunksAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            var searchDocuments = chunks.Select(ToSearchDocument).ToArray();
            if (searchDocuments.Length == 0)
            {
                return;
            }

            var response = await _client.MergeOrUploadDocumentsAsync(searchDocuments, cancellationToken: cancellationToken);
            var failed = response.Value.Results.Where(result => !result.Succeeded).ToArray();
            if (failed.Length > 0)
            {
                throw new InvalidOperationException($"Azure AI Search failed to index {failed.Length} of {searchDocuments.Length} chunks.");
            }

            _logger.LogInformation("Indexed {ChunkCount} document chunks into Azure AI Search index {IndexName}.", searchDocuments.Length, _indexName);
        }

        public async Task<List<string>> VectorSearchAsync(float[] embedding)
        {
            var options = new SearchOptions
            {
                Size = 5,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                {
                    new VectorizedQuery(embedding)
                    {
                        KNearestNeighborsCount = 5,
                        Fields = { "embedding" }
                    }
                }
                }
            };

            var results = await _client.SearchAsync<SearchDocument>("*", options);
            return results.Value.GetResults()
               .Select(r => r.Document.TryGetValue("content", out var content) ? content?.ToString() : null)
               .Where(content => !string.IsNullOrWhiteSpace(content))
               .Select(content => content!)
               .ToList();
        }

        public async Task CreateIndexIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var existingIndex = await _indexClient.GetIndexAsync(_indexName, cancellationToken);
                var missingFields = GetRequiredVersioningFieldNames()
                    .Except(existingIndex.Value.Fields.Select(field => field.Name), StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (missingFields.Length == 0)
                {
                    return;
                }

                _logger.LogWarning(
                    "Azure AI Search index {IndexName} already exists but is missing versioning fields: {MissingFields}. Create a new index or migrate the schema before relying on document version filters.",
                    _indexName,
                    string.Join(", ", missingFields));

                throw new InvalidOperationException(
                    $"Azure AI Search index '{_indexName}' is missing required document versioning fields: {string.Join(", ", missingFields)}. Create a new index or migrate the existing index schema before ingestion.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("Azure AI Search index {IndexName} does not exist and will be created.", _indexName);
            }

            var fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("content") { AnalyzerName = LexicalAnalyzerName.EnLucene },
                new SearchableField("fileName") { IsFilterable = true, IsSortable = true },
                new SearchableField("sourceFileName") { IsFilterable = true, IsSortable = true },
                new SimpleField("sourceUri", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("documentId", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SimpleField("documentVersion", SearchFieldDataType.String) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SimpleField("ingestedAt", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("contentHash", SearchFieldDataType.String) { IsFilterable = true },
                new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _vectorDimensions,
                    VectorSearchProfileName = "vector-profile"
                }
            };

            var vectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("vector-config")
            {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 4,
                            EfConstruction = 400
                        }
                    }
                },

                Profiles =
                {
                    new VectorSearchProfile(
                        name: "vector-profile",
                        algorithmConfigurationName: "vector-config")
                }
            };

            var index = new SearchIndex(_indexName, fields)
            {
                VectorSearch = vectorSearch
            };

            await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
            _logger.LogInformation("Created Azure AI Search index {IndexName} with document versioning fields.", _indexName);
        }

        private static SearchDocument ToSearchDocument(DocumentChunk chunk)
        {
            return new SearchDocument
            {
                ["id"] = chunk.Id,
                ["content"] = chunk.Content,
                ["fileName"] = chunk.SourceFileName,
                ["sourceFileName"] = chunk.SourceFileName,
                ["sourceUri"] = chunk.SourceUri,
                ["documentId"] = chunk.DocumentId,
                ["documentVersion"] = chunk.DocumentVersion,
                ["chunkIndex"] = chunk.ChunkIndex,
                ["ingestedAt"] = chunk.IngestedAt,
                ["contentHash"] = chunk.ContentHash,
                ["embedding"] = chunk.Embedding
            };
        }

        private static string[] GetRequiredVersioningFieldNames()
        {
            return
            [
                "documentId",
                "documentVersion",
                "chunkIndex",
                "sourceFileName",
                "sourceUri",
                "ingestedAt",
                "contentHash"
            ];
        }
    }
}
