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
    public async Task Run([BlobTrigger("samples-workitems/{name}", Connection = "")] Stream blobStream, string name)
    {
        using var blobStreamReader = new StreamReader(blobStream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);

        await _pdfIngestService.IngestAsync(blobStream, name);

        _logger.LogInformation($"Blob {name} ingested successfully");
       
    }
}