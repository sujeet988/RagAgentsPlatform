using RagAgents.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    /// <summary>
    /// Fallback no-op PDF ingest service for environments where Document AI is not configured.
    /// </summary>
    public class NoOpPdfIngestService : IDocumentIngestService
    {
        public Task IngestAsync(Stream pdf, string fileName, CancellationToken cancellationToken = default)
        {
            // intentionally do nothing in local/dev where DocumentAI is not configured
            return Task.CompletedTask;
        }

     
    }
}
