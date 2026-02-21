using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Services;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace RagAgents.Functions.Functions;

public class PdfBlobIngestFunction
{
    private readonly IPdfIngestService _pdfIngestService;
    private readonly ILogger<PdfBlobIngestFunction> _logger;

    public PdfBlobIngestFunction(IPdfIngestService pdfIngestService, ILogger<PdfBlobIngestFunction> logger)
    {
        _pdfIngestService = pdfIngestService;
        _logger = logger;
    }

    [Function(nameof(PdfBlobIngestFunction))]
    public async Task Run(
        [BlobTrigger("pdfcontainer/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, 
        string name)
    {
        _logger.LogInformation("Blob trigger started: {FileName}", name);

        try
        {
            using var ms = new MemoryStream();
            await blobStream.CopyToAsync(ms);
            ms.Position = 0;

            await _pdfIngestService.IngestAsync(ms, name);

            _logger.LogInformation("Blob {FileName} ingested successfully", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest blob {FileName}", name);
            throw;
        }
    }
}