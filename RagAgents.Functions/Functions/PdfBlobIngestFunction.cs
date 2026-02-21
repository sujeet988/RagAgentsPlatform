using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RagAgents.Core.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace RagAgents.Functions.Functions;

public class PdfBlobIngestFunction
{
    private readonly IPdfIngestService _pdfIngestService;
    private readonly IJobTrackingService _jobTracking;
    private readonly ILogger<PdfBlobIngestFunction> _logger;

    public PdfBlobIngestFunction(
        IPdfIngestService pdfIngestService,
        IJobTrackingService jobTracking,
        ILogger<PdfBlobIngestFunction> logger)
    {
        _pdfIngestService = pdfIngestService;
        _jobTracking = jobTracking;
        _logger = logger;
    }

    [Function(nameof(PdfBlobIngestFunction))]
    public async Task Run(
        [BlobTrigger("pdfcontainer/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name,
        IDictionary<string, string> metadata,
        long length)
    {
        _logger.LogInformation("Blob trigger started: {FileName}, Size: {Size} bytes", name, length);

        try
        {
            // 1. Create job tracking record
            var userId = metadata.TryGetValue("userId", out var uid) ? uid : "system";
            var blobUrl = $"{System.Environment.GetEnvironmentVariable("BlobStorageUrl")}/pdfcontainer/{name}";
            
            var job = await _jobTracking.CreateJobAsync(name, blobUrl, userId, length);
            _logger.LogInformation("Created job {JobId} for {FileName}", job.Id, name);

            // 2. Process the document
            using var ms = new MemoryStream();
            await blobStream.CopyToAsync(ms);
            ms.Position = 0;

            await _pdfIngestService.IngestAsync(ms, name);

            _logger.LogInformation("Blob {FileName} ingested successfully. Job: {JobId}", name, job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest blob {FileName}", name);
            throw;
        }
    }
}