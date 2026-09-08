using System.IO;
using System.Threading.Tasks;
using RagAgents.Core.Models;

namespace RagAgents.Functions.Services
{
    /// <summary>
    /// Facade used by Azure Functions to orchestrate ingestion and related operations.
    /// Keeps function handlers thin and delegates work to core services.
    /// </summary>
    public interface IFunctionIngestService
    {
        Task<DocumentIngestResult> IngestBlobAsync(
            Stream blobStream,
            string name,
            Uri? sourceUri = null,
            string? documentVersion = null,
            CancellationToken cancellationToken = default);
    }
}
