using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using RagAgents.Core.Helpers;
using RagAgents.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class PdfIngestService : IPdfIngestService
    {
        private readonly DocumentAnalysisClient _docClient;
        private readonly IAzureOpenAIService _openAI;      // Use AzureOpenAIService (2.1.0)
        private readonly IAzureSearchService _search;      // Azure.Search.Documents based service
        public PdfIngestService(DocumentAnalysisClient docClient,IAzureOpenAIService openAI,IAzureSearchService search)
        {
            _docClient = docClient;
            _openAI = openAI;
            _search = search;

        }
        public async Task IngestAsync(Stream pdf, string fileName)
        {
            try
            {


                // 1️⃣ Analyze PDF using prebuilt layout model
                var operation = await _docClient.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-layout",
                    pdf);

                // 2️⃣ Extract all text from the document
                var text = string.Join("\n",
                    operation.Value.Pages
                        .SelectMany(p => p.Lines)
                        .Select(l => l.Content));

                // 3️⃣ Split text into manageable chunks
                var chunks = ChunkingHelper.SplitTextByOverLap(text, 800, 100);

                // First Check index exists or not in azure ai seacch if not  it will create index
                await _search.CreateIndexIfNotExistsAsync();

                // 4️⃣ For each chunk: create embedding and index in Azure Search
                foreach (var chunk in chunks)
                {
                    // Generate embedding using Azure OpenAI
                    var embedding = await _openAI.CreateEmbeddingAsync(chunk);

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
