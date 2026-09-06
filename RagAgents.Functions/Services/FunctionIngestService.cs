using RagAgents.Core.Interfaces;
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

        public async Task IngestBlobAsync(Stream blobStream, string name)
        {
            await _documentIngestService.IngestAsync(blobStream, name);
        }
    }
}
