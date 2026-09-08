using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Functions.Services;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Functions.Services
{
    public class FunctionIngestService : IFunctionIngestService
    {
        private readonly IDocumentIngestService _documentIngestService;

        public FunctionIngestService(IDocumentIngestService documentIngestService)
        {
            _documentIngestService = documentIngestService;
        }

        public Task<DocumentIngestResult> IngestBlobAsync(
            Stream blobStream,
            string name,
            Uri? sourceUri = null,
            string? documentVersion = null,
            CancellationToken cancellationToken = default)
        {
            var request = new DocumentIngestRequest(
                Content: blobStream,
                FileName: name,
                SourceUri: sourceUri?.ToString(),
                DocumentVersion: documentVersion);

            return _documentIngestService.IngestAsync(request, cancellationToken);
        }
    }
}
