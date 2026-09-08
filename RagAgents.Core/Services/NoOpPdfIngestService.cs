using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    /// <summary>
    /// Fallback no-op PDF ingest service for environments where Document AI is not configured.
    /// </summary>
    public class NoOpPdfIngestService : IDocumentIngestService
    {
        public Task<DocumentIngestResult> IngestAsync(DocumentIngestRequest request, CancellationToken cancellationToken = default)
        {
            // intentionally do nothing in local/dev where DocumentAI is not configured
            return Task.FromResult(new DocumentIngestResult(
                request.DocumentId ?? request.FileName,
                request.DocumentVersion ?? "noop",
                request.FileName,
                0,
                DateTimeOffset.UtcNow));
        }

     
    }
}
