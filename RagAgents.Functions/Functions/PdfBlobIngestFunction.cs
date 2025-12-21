using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RagAgents.Functions.Functions;

public class PdfBlobIngestFunction
{
    private readonly ILogger<PdfBlobIngestFunction> _logger;

    public PdfBlobIngestFunction(ILogger<PdfBlobIngestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(PdfBlobIngestFunction))]
    public async Task Run([BlobTrigger("samples-workitems/{name}", Connection = "")] Stream stream, string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}", name, content);
    }
}