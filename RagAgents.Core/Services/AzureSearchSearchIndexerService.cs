using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class AzureSearchSearchIndexerService : ISearchIndexer
    {
        private readonly SearchClient _client;
        private readonly SearchIndexClient _indexClient;
        private readonly string _indexName ;
        private readonly string _endPoint;
        private readonly string _key;
        public AzureSearchSearchIndexerService(IOptions<AzureSearchAIOptions> options) {
            var cfg = options.Value;
            _client = new SearchClient(
                new Uri(cfg.Endpoint),
                cfg.IndexName,
                new AzureKeyCredential(cfg.Key));
            _indexName= cfg.IndexName;
            _indexClient  = new SearchIndexClient(
                new Uri(cfg.Endpoint),
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

        public async Task CreateIndexIfNotExistsAsync()
        {
            try
            {


                // 🔹 Fast existence check
                await foreach (var name in _indexClient.GetIndexNamesAsync())
                {
                    if (name == _indexName)
                        return;
                }

                var fields = new List<SearchField>
            {
            new SearchField("id", SearchFieldDataType.String)
            {
            IsKey = true
            },

            new SearchField("content", SearchFieldDataType.String)
            {
            IsSearchable = true
            },

            new SearchField("fileName", SearchFieldDataType.String)
            {
            IsSearchable = true,
            IsFilterable = true
            },

            new SearchField(
            "embedding",
            SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
            IsSearchable = true,
            VectorSearchDimensions = 3072, // is default value 
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

                await _indexClient.CreateOrUpdateIndexAsync(index);
            }
            catch (Exception ex)
            {
                throw ex;
            }



        }
    }
}
