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
        public PdfIngestService()
        {
            
        }
        public async Task IngestAsync(Stream pdf, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
