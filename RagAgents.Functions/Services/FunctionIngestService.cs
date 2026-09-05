using RagAgents.Core.Interfaces;
using RagAgents.Functions.Services;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Functions.Services
{
    public class FunctionIngestService : IFunctionIngestService
    {
        private readonly IDocumentIngestService _pdfIngestService;

        public FunctionIngestService(IDocumentIngestService pdfIngestService)
        {
            _pdfIngestService = pdfIngestService;
        }

        public async Task IngestBlobAsync(Stream blobStream, string name)
        {
            await _pdfIngestService.IngestAsync(blobStream, name);
        }
    }
}
