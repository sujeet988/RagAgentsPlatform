using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class PdfIngestService : IPdfIngestService
    {
        private readonly DocumentAnalysisClient _docClient;
        private readonly IAzureOpenAIService _openAI;
        private readonly IAzureSearchService _search;
        private readonly IJobTrackingService _jobTracking;
        private readonly ILogger<PdfIngestService> _logger;
        private readonly TelemetryClient _telemetryClient;

        private const int BatchSize = 100; // Azure OpenAI supports up to 2048, but 100 is safer
        private const int MaxParallelTasks = 5; // Concurrent batches
        private const int ChunkSize = 800;
        private const int ChunkOverlap = 100;

        public PdfIngestService(
            DocumentAnalysisClient docClient,
            IAzureOpenAIService openAI,
            IAzureSearchService search,
            IJobTrackingService jobTracking,
            ILogger<PdfIngestService> logger,
            TelemetryClient telemetryClient)
        {
            _docClient = docClient;
            _openAI = openAI;
            _search = search;
            _jobTracking = jobTracking;
            _logger = logger;
            _telemetryClient = telemetryClient;
        }
        public async Task IngestAsync(Stream pdf, string fileName)
        {
            var overallSw = Stopwatch.StartNew();
            IngestionJob? job = null;

            // Start custom telemetry operation
            using var operation = _telemetryClient.StartOperation<RequestTelemetry>($"IngestPdf:{fileName}");
            operation.Telemetry.Properties["FileName"] = fileName;

            try
            {
                // Get or create job
                job = await _jobTracking.GetJobByFileNameAsync(fileName);
                if (job != null)
                {
                    job.Status = IngestionStatus.Processing;
                    job.StartedAt = DateTime.UtcNow;
                    await _jobTracking.UpdateJobAsync(job);
                }

                _logger.LogInformation("Starting ingestion for {FileName}", fileName);

                // 1️⃣ Extract text from PDF
                var sw = Stopwatch.StartNew();
                var text = await ExtractTextAsync(pdf);
                var extractionTime = sw.ElapsedMilliseconds;
                _telemetryClient.TrackMetric("PdfExtraction.Duration", extractionTime);
                _logger.LogInformation("Extracted text from {FileName} in {Duration}ms", fileName, extractionTime);

                // 2️⃣ Split into chunks
                sw.Restart();
                var chunks = SplitText(text, ChunkSize, ChunkOverlap).ToList();
                var chunkingTime = sw.ElapsedMilliseconds;
                _telemetryClient.TrackMetric("Chunking.Duration", chunkingTime);
                _telemetryClient.TrackMetric("Chunking.Count", chunks.Count);
                _logger.LogInformation("Split {FileName} into {ChunkCount} chunks in {Duration}ms", 
                    fileName, chunks.Count, chunkingTime);

                if (job != null)
                {
                    job.TotalChunks = chunks.Count;
                    job.ChunkingTimeMs = chunkingTime;
                    job.ExtractionTimeMs = extractionTime;
                    await _jobTracking.UpdateJobAsync(job);
                }

                // 3️⃣ Ensure index exists
                await _search.CreateIndexIfNotExistsAsync();

                // 4️⃣ Process chunks in batches with parallel execution
                sw.Restart();
                var processedChunks = await ProcessChunksInBatchesAsync(chunks, fileName, job);
                var embeddingAndIndexingTime = sw.ElapsedMilliseconds;
                _telemetryClient.TrackMetric("EmbeddingAndIndexing.Duration", embeddingAndIndexingTime);

                // 5️⃣ Update job completion
                overallSw.Stop();
                if (job != null)
                {
                    job.Status = processedChunks == chunks.Count 
                        ? IngestionStatus.Completed 
                        : IngestionStatus.PartiallyCompleted;
                    job.CompletedAt = DateTime.UtcNow;
                    job.ProcessedChunks = processedChunks;
                    job.FailedChunks = chunks.Count - processedChunks;
                    job.EmbeddingTimeMs = embeddingAndIndexingTime;
                    job.TotalTimeMs = overallSw.ElapsedMilliseconds;
                    await _jobTracking.UpdateJobAsync(job);
                }

                _logger.LogInformation(
                    "Completed ingestion for {FileName}: {Processed}/{Total} chunks in {Duration}ms",
                    fileName, processedChunks, chunks.Count, overallSw.ElapsedMilliseconds);

                operation.Telemetry.Success = true;
            }
            catch (Exception ex)
            {
                overallSw.Stop();
                _logger.LogError(ex, "Failed to ingest {FileName} after {Duration}ms", 
                    fileName, overallSw.ElapsedMilliseconds);

                if (job != null)
                {
                    job.Status = IngestionStatus.Failed;
                    job.ErrorMessage = ex.Message;
                    job.CompletedAt = DateTime.UtcNow;
                    job.TotalTimeMs = overallSw.ElapsedMilliseconds;
                    await _jobTracking.UpdateJobAsync(job);
                }

                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    ["FileName"] = fileName,
                    ["Duration"] = overallSw.ElapsedMilliseconds.ToString()
                });

                operation.Telemetry.Success = false;
                throw;
            }
        }

        private async Task<string> ExtractTextAsync(Stream pdf)
        {
            var operation = await _docClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                pdf);

            return string.Join("\n",
                operation.Value.Pages
                    .SelectMany(p => p.Lines)
                    .Select(l => l.Content));
        }

        private async Task<int> ProcessChunksInBatchesAsync(
            List<string> chunks, 
            string fileName, 
            IngestionJob? job)
        {
            var batches = chunks
                .Select((chunk, index) => new { Chunk = chunk, Index = index })
                .GroupBy(x => x.Index / BatchSize)
                .Select(g => g.Select(x => new { x.Chunk, x.Index }).ToList())
                .ToList();

            _logger.LogInformation(
                "Processing {ChunkCount} chunks in {BatchCount} batches with {Parallelism} max parallel tasks",
                chunks.Count, batches.Count, MaxParallelTasks);

            var processedCount = 0;
            var semaphore = new System.Threading.SemaphoreSlim(MaxParallelTasks);

            var tasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var batchChunks = batch.Select(x => x.Chunk).ToList();
                    var processed = await ProcessBatchAsync(batchChunks, fileName);

                    // Update progress
                    if (job != null)
                    {
                        lock (job)
                        {
                            job.ProcessedChunks += processed;
                        }
                        await _jobTracking.UpdateJobAsync(job);
                    }

                    return processed;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process batch for {FileName}", fileName);
                    return 0;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            processedCount = results.Sum();

            return processedCount;
        }

        private async Task<int> ProcessBatchAsync(List<string> chunks, string fileName)
        {
            try
            {
                // Create embeddings for all chunks in batch (single API call)
                var embeddingTasks = chunks.Select(chunk => _openAI.CreateEmbeddingAsync(chunk));
                var embeddings = await Task.WhenAll(embeddingTasks);

                // Index all chunks in batch
                var documents = chunks.Select((chunk, i) => new
                {
                    id = Guid.NewGuid().ToString(),
                    content = chunk,
                    embedding = embeddings[i],
                    fileName = fileName,
                    chunkIndex = i,
                    indexedAt = DateTime.UtcNow
                }).ToList();

                // Index in batch (Azure Search supports batch operations)
                foreach (var doc in documents)
                {
                    await _search.IndexAsync(doc);
                }

                _logger.LogDebug("Indexed batch of {Count} chunks for {FileName}", chunks.Count, fileName);
                return chunks.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch of {Count} chunks", chunks.Count);
                _telemetryClient.TrackException(ex);
                return 0;
            }
        }

        // Helper: split long text into chunks with overlap
        private static IEnumerable<string> SplitText(string text, int size, int overlap)
        {
            for (int i = 0; i < text.Length; i += size - overlap)
                yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }
    
}
