# Testing Guide - Production Improvements

## Prerequisites
1. Azure resources configured:
   - Cosmos DB with `ingestion-jobs` container
   - Application Insights
   - Azure OpenAI, Azure AI Search, Document Intelligence
   - Blob Storage

2. Configuration updated:
   - User Secrets for local development
   - Application Insights connection string
   - All Azure service endpoints and keys

## Local Testing Steps

### Step 1: Restore Packages
```powershell
cd c:\Users\sujeet\source\repos\RagAgents
dotnet restore
dotnet build
```

### Step 2: Configure User Secrets (API)
```powershell
cd RagAgents.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "your-api-key"
dotnet user-secrets set "AzureOpenAI:AzureCosmos:Endpoint" "https://your-cosmos.documents.azure.com:443/"
dotnet user-secrets set "AzureOpenAI:AzureCosmos:Key" "your-cosmos-key"
# ... add other secrets
```

### Step 3: Configure User Secrets (Functions)
```powershell
cd ..\RagAgents.Functions
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "your-api-key"
dotnet user-secrets set "AzureOpenAI:AzureCosmos:Endpoint" "https://your-cosmos.documents.azure.com:443/"
dotnet user-secrets set "AzureOpenAI:AzureCosmos:Key" "your-cosmos-key"
# ... add other secrets
```

### Step 4: Start Azure Storage Emulator
```powershell
# Start Azurite (Azure Storage Emulator)
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

Or use Docker:
```powershell
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

### Step 5: Run the Function
```powershell
cd RagAgents.Functions
func start
```

Expected output:
```
Azure Functions Core Tools
...
Functions:
  PdfBlobIngestFunction: blobTrigger
```

### Step 6: Upload Test PDF
Using Azure Storage Explorer or CLI:

```powershell
# Install Azure CLI if not already installed
# az storage blob upload --account-name <storage-account> --container-name pdfcontainer --file test.pdf --name test.pdf

# Or use Azure Storage Explorer GUI
# 1. Connect to local storage (Azurite)
# 2. Create container "pdfcontainer"
# 3. Upload a test PDF file
# 4. Add metadata: userId = "test-user-123"
```

### Step 7: Monitor Execution

#### Check Function Logs
The function console will show:
```
[INFO] Blob trigger started: test.pdf, Size: 123456 bytes
[INFO] Created job abc123-def456 for test.pdf
[INFO] Starting PDF ingestion: test.pdf
[INFO] Text extraction completed: 5 pages, 12500 characters in 1234ms
[INFO] Chunking completed: 15 chunks in 45ms
[INFO] Processing batch 1/1 (15 chunks)
[INFO] Batch 1 completed: 15 embeddings generated in 890ms
[INFO] Indexing 15 documents to Azure AI Search
[INFO] PDF ingestion completed successfully in 3456ms
[INFO] Blob test.pdf ingested successfully. Job: abc123-def456
```

#### Check Cosmos DB
Query the `ingestion-jobs` container:
```sql
SELECT * FROM c WHERE c.fileName = "test.pdf"
```

Expected result:
```json
{
  "id": "abc123-def456",
  "fileName": "test.pdf",
  "userId": "test-user-123",
  "status": "Completed",
  "totalChunks": 15,
  "processedChunks": 15,
  "failedChunks": 0,
  "extractionTimeMs": 1234,
  "chunkingTimeMs": 45,
  "embeddingTimeMs": 890,
  "indexingTimeMs": 567,
  "totalTimeMs": 3456,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:03Z"
}
```

#### Check Application Insights
Navigate to Application Insights in Azure Portal:

1. **Metrics**:
   - Custom Metrics → `PdfIngest.TotalDuration`
   - Custom Metrics → `PdfIngest.ChunkCount`

2. **Logs** (Query):
```kql
traces
| where message contains "PDF ingestion"
| project timestamp, message, severityLevel
| order by timestamp desc
```

3. **Application Map**: Verify dependencies (OpenAI, Search, Document AI, Cosmos)

## Test Scenarios

### Scenario 1: Small PDF (1-5 pages)
- **Expected**: ~2-5 seconds total
- **Chunks**: ~5-20 chunks
- **Batches**: 1 batch
- **Parallel**: No parallelization needed

### Scenario 2: Medium PDF (20-50 pages)
- **Expected**: ~8-15 seconds total
- **Chunks**: ~80-200 chunks
- **Batches**: 1-2 batches
- **Parallel**: 1-2 concurrent batches

### Scenario 3: Large PDF (100+ pages)
- **Expected**: ~20-40 seconds total
- **Chunks**: ~400-1000 chunks
- **Batches**: 4-10 batches
- **Parallel**: 5 concurrent batches (MaxParallelTasks)

### Scenario 4: Error Handling
Test with:
- Invalid PDF file (corrupted)
- Non-PDF file (image, text)
- Very large file (>100MB)
- Network timeout scenarios

Expected behavior:
- Job status updates to "Failed"
- Error message logged
- Exception tracked in Application Insights
- Retry count incremented

## Performance Benchmarks

### Target Metrics
| Document Size | Chunks | Expected Time | Max Time |
|--------------|--------|---------------|----------|
| 1-5 pages    | 5-20   | 2-5 sec       | 10 sec   |
| 10-20 pages  | 40-80  | 5-10 sec      | 20 sec   |
| 50-100 pages | 200-400| 15-25 sec     | 40 sec   |
| 200+ pages   | 800+   | 30-60 sec     | 120 sec  |

### Key Performance Indicators (KPIs)
- **Success Rate**: >95%
- **P95 Latency**: <30 seconds for 50-page document
- **Chunk Processing Rate**: >100 chunks/second
- **API Error Rate**: <1%

## Troubleshooting

### Issue: Function not triggering
- **Check**: Blob container name is "pdfcontainer"
- **Check**: Connection string in local.settings.json
- **Check**: Azurite is running

### Issue: "CosmosClient not registered"
- **Solution**: Verify `AddAzureClients()` is called in Program.cs
- **Solution**: Check Cosmos DB configuration in options

### Issue: "TelemetryClient not found"
- **Solution**: Ensure `AddApplicationInsightsTelemetryWorkerService()` is registered
- **Solution**: Verify APPLICATIONINSIGHTS_CONNECTION_STRING is set

### Issue: 429 Rate Limit Errors
- **Solution**: Reduce `MaxParallelTasks` from 5 to 3
- **Solution**: Check Azure OpenAI quota limits
- **Solution**: Add retry policy with exponential backoff

### Issue: Slow Performance
- **Check**: Network latency to Azure services
- **Check**: Document AI service tier (Standard vs Basic)
- **Check**: Chunk size and batch size configuration
- **Optimize**: Increase `MaxParallelTasks` if quota allows

## Validation Checklist

- [ ] Function builds without errors
- [ ] Function starts successfully
- [ ] Blob trigger fires on upload
- [ ] Job created in Cosmos DB (Status: Queued)
- [ ] Job updated during processing (Status: Processing)
- [ ] Text extracted from PDF
- [ ] Chunks created correctly
- [ ] Embeddings generated in batches
- [ ] Documents indexed to Azure AI Search
- [ ] Job completed successfully (Status: Completed)
- [ ] All timing metrics populated
- [ ] Telemetry visible in Application Insights
- [ ] No errors in function logs
- [ ] Search works with ingested content

## Next Steps

After local testing:
1. Deploy to Azure Function App
2. Configure managed identity
3. Set up alerts in Application Insights
4. Monitor production metrics
5. Optimize based on real usage patterns

## Sample Test Commands

### Query Jobs API (if implemented)
```powershell
# Get all jobs for user
curl -X GET "https://localhost:7071/api/jobs/user/test-user-123" `
     -H "Authorization: Bearer <token>"

# Get specific job
curl -X GET "https://localhost:7071/api/jobs/abc123-def456" `
     -H "Authorization: Bearer <token>"

# Get failed jobs
curl -X GET "https://localhost:7071/api/jobs/status/Failed" `
     -H "Authorization: Bearer <token>"
```

### Manual Job Creation (for testing)
```csharp
var job = await jobTrackingService.CreateJobAsync(
    fileName: "test.pdf",
    blobUrl: "https://storage.blob.core.windows.net/pdfcontainer/test.pdf",
    userId: "test-user-123",
    fileSizeBytes: 123456
);
```

---

**Ready to test!** 🚀

For production deployment, see [PRODUCTION_IMPROVEMENTS.md](PRODUCTION_IMPROVEMENTS.md) deployment checklist.
