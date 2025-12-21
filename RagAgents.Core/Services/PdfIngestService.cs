using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
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
        private readonly AzureOpenAIService _openAI;      // Use AzureOpenAIService (2.1.0)
        private readonly AzureSearchService _search;      // Azure.Search.Documents based service
        public PdfIngestService(DocumentAnalysisClient docClient,AzureOpenAIService openAI,AzureSearchService search)
        {
            _docClient = docClient;
            _openAI = openAI;
            _search = search;

        }
        public async Task IngestAsync(Stream pdf, string fileName)
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
            var chunks = SplitText(text, 800, 100);

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

        // Helper: split long text into chunks with overlap
        private static IEnumerable<string> SplitText(string text, int size, int overlap)
        {
            for (int i = 0; i < text.Length; i += size - overlap)
                yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }
    
}
