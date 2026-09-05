using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Functions.Services
{
    /// <summary>
    /// Facade used by Azure Functions to orchestrate ingestion and related operations.
    /// Keeps function handlers thin and delegates work to core services.
    /// </summary>
    public interface IFunctionIngestService
    {
        Task IngestBlobAsync(Stream blobStream, string name);
    }
}
