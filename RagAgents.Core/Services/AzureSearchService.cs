using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class AzureSearchService: IAzureSearchService
    {
        private readonly SearchClient _client;
        public AzureSearchService(IOptions<AzureSearchAIOptions> options) {
            var cfg = options.Value;
            _client = new SearchClient(
                new Uri(cfg.Endpoint),
                cfg.IndexName,
                new AzureKeyCredential(cfg.Key));
        }

        public async Task IndexAsync<T>(T document)
        {
            try
            {
                await _client.UploadDocumentsAsync(new[] { document });
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
               .Select(r => r.Document["content"].ToString())
               .ToList();
        }
    }
}
