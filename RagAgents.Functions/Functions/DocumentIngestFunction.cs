using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Services;
using RagAgents.Functions.Services;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace RagAgents.Functions.Functions;

public class DocumentIngestFunction
{
    private readonly IFunctionIngestService  _functionIngestService;
    private readonly ILogger<DocumentIngestFunction> _logger;

    public DocumentIngestFunction(IFunctionIngestService functionIngestService, ILogger<DocumentIngestFunction> logger)
    {
        _functionIngestService = functionIngestService;
        _logger = logger;
    }

    [Function(nameof(DocumentIngestFunction))]
    public async Task Run([BlobTrigger("pdfcontainer/{name}", Connection = "StorageConnectiontest")] Stream blobStream, string name)
    {
       // using var blobStreamReader = new StreamReader(blobStream);
       // var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, "content");
        using var ms = new MemoryStream();
        await blobStream.CopyToAsync(ms);
        ms.Position = 0;

        await _functionIngestService.IngestBlobAsync(ms, name);
        _logger.LogInformation($"Blob {name} ingested successfully");
       
    }
}