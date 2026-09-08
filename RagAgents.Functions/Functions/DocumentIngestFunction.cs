using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RagAgents.Functions.Services;
using System.Diagnostics.CodeAnalysis;

namespace RagAgents.Functions.Functions;

[ExcludeFromCodeCoverage]
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
    public async Task Run(
        [BlobTrigger("pdfcontainer/{name}", Connection = "StorageConnectiontest")] BlobClient blobClient,
        string name,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["BlobName"] = name,
            ["InvocationId"] = context.InvocationId
        });

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var version = ResolveVersion(properties.Value.VersionId, properties.Value.ETag);

            _logger.LogInformation(
                "Processing blob {BlobName}. Size: {Size} bytes; Version: {DocumentVersion}.",
                name,
                properties.Value.ContentLength,
                version);

            await using var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            var result = await _functionIngestService.IngestBlobAsync(
                stream,
                name,
                blobClient.Uri,
                version,
                cancellationToken);

            _logger.LogInformation(
                "Blob {BlobName} ingested successfully. DocumentId: {DocumentId}; Version: {DocumentVersion}; Chunks: {ChunkCount}.",
                name,
                result.DocumentId,
                result.DocumentVersion,
                result.ChunkCount);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure request failed while ingesting blob {BlobName}. Status: {Status}; ErrorCode: {ErrorCode}.", name, ex.Status, ex.ErrorCode);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Blob ingestion was cancelled for {BlobName}.", name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure while ingesting blob {BlobName}.", name);
            throw;
        }
    }

    private static string? ResolveVersion(string? blobVersionId, ETag eTag)
    {
        return !string.IsNullOrWhiteSpace(blobVersionId)
            ? blobVersionId
            : eTag.ToString().Trim('"');
    }
}