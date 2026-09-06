using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using RagAgents.Core.Helpers;
using RagAgents.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class DocumentIngestService : IDocumentIngestService
    {
        private readonly IDocumentTextExtractor _textExtractor;
        private readonly IOpenAIEmbeddingService  _openAIEmbeddingService;      // Use AzureOpenAIService (2.1.0)
        private readonly ISearchIndexer _search;      // Azure.Search.Documents based service
        public DocumentIngestService(IDocumentTextExtractor textExtractor, IOpenAIEmbeddingService openAIEmbeddingService, ISearchIndexer search)
        {
            _textExtractor = textExtractor;
            _openAIEmbeddingService = openAIEmbeddingService;
            _search = search;

        }
        public async Task IngestAsync(Stream document, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {


                // 1. Extract text
                var text = await _textExtractor.ExtractTextAsync(
                    document,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(text))
                    return;

                // 3️⃣ Split text into manageable chunks
                var chunks = ChunkingHelper.SplitTextByOverLap(text, 800, 100);

                // First Check index exists or not in azure ai seacch if not  it will create index
                await _search.CreateIndexIfNotExistsAsync();

                // 4️⃣ For each chunk: create embedding and index in Azure Search
                foreach (var chunk in chunks)
                {
                    // Generate embedding using Azure OpenAI
                    var embedding = await _openAIEmbeddingService.CreateEmbeddingAsync(chunk);

                    // Index chunk in Azure AI Search
                    await _search.IndexAsync(new
                    {
                        id = Guid.NewGuid().ToString(),
                        content = chunk,
                        embedding = embedding,
                        fileName = fileName
                    });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }

       
    }
    
}
