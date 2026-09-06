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
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        private readonly DocumentAnalysisClient _documentClient;
        public DocumentTextExtractor(DocumentAnalysisClient documentClient)
        {
            _documentClient = documentClient;
        }
        public async Task<string> ExtractTextAsync(Stream document, CancellationToken cancellationToken = default)
        {
            var operation = await _documentClient.AnalyzeDocumentAsync(
           WaitUntil.Completed,
           "prebuilt-layout",
           document,
           cancellationToken: cancellationToken);

            return string.Join(
                Environment.NewLine,
                operation.Value.Pages
                    .SelectMany(page => page.Lines)
                    .Select(line => line.Content));
        }
    }
}
