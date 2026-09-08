# Ingestion Refactor Summary

## Purpose

This document summarizes the production-ready ingestion refactor for the RAG platform.

The Azure Function now delegates ingestion to the core service pipeline:

**Blob → text extraction → chunking → embeddings → Azure AI Search vector storage**

## Updated Flow

1. `DocumentIngestFunction` is triggered by a blob in `pdfcontainer/{name}`.
2. The function receives a `BlobClient` and reads blob properties.
3. Blob versioning metadata is resolved from `VersionId` or `ETag`.
4. `FunctionIngestService` creates a `DocumentIngestRequest`.
5. `DocumentIngestService` performs the core ingestion workflow:
   - extracts text through `IDocumentTextExtractor`
   - splits text into overlapping chunks
   - creates embeddings through `IOpenAIEmbeddingService`
   - indexes version-aware chunks through `ISearchIndexer`

## Production Improvements

- Function handler is thin and delegates business logic to core services.
- Core ingestion now uses typed request/result models.
- Cancellation tokens flow through the pipeline.
- Structured logging scopes include blob name and invocation id.
- Azure SDK failures are logged with status and error code.
- Empty extraction/chunking results are handled safely.
- Chunking options are configurable through `Ingestion` configuration.
- Required configuration is validated at startup.
- Embeddings are generated in batches for better throughput.
- Azure AI Search indexing uses `MergeOrUploadDocumentsAsync` for idempotency.

## Azure AI Search Versioning Fields

The search index now tracks document and version metadata per chunk:

- `id` — deterministic chunk key
- `content` — chunk text
- `fileName` — backward-compatible source file field
- `sourceFileName` — source blob/file name
- `sourceUri` — blob URI
- `documentId` — stable document identifier
- `documentVersion` — blob `VersionId`, `ETag`, or content hash fallback
- `chunkIndex` — chunk sequence number
- `ingestedAt` — ingestion timestamp
- `contentHash` — SHA-256 hash of extracted document text
- `embedding` — vector embedding

## Idempotency and Versioning

Chunk IDs are deterministic from:

`documentId + documentVersion + chunkIndex`

This allows the same document version to be reprocessed without creating duplicate chunk records, while different versions of the same document are stored independently.

## Configuration

Required settings:

- `AzureOpenAI:Endpoint`
- `AzureOpenAI:Key`
- `AzureOpenAI:EmbeddingDeployment`
- `AzureSearch:Endpoint`
- `AzureSearch:Key`
- `AzureSearch:IndexName`
- `DocumentAI:Endpoint`
- `DocumentAI:Key`

Optional settings:

- `AzureSearch:VectorDimensions` defaults to `3072`
- `Ingestion:ChunkSize` defaults to `800`
- `Ingestion:ChunkOverlap` defaults to `100`
- `Ingestion:EmbeddingBatchSize` defaults to `16`

## Existing Index Migration Note

If `AzureSearch:IndexName` points to an existing index created with the old schema, ingestion now fails fast with a clear error if required versioning fields are missing.

Recommended options:

1. Create a new index name for the versioned schema.
2. Recreate the existing index if data can be regenerated.
3. Migrate data into a new index, then update application configuration.

## Validation

The solution was validated with:

`dotnet build "RagAgents.sln"`

Result: build succeeded.

Remaining warnings are unrelated nullable warnings in existing agent files.
