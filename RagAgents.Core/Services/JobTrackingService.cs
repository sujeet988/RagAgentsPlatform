using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class JobTrackingService : IJobTrackingService
    {
        private readonly Container _container;
        private readonly ILogger<JobTrackingService> _logger;

        public JobTrackingService(CosmosClient cosmosClient, IOptions<AzureCosmosOptions> options, ILogger<JobTrackingService> logger)
        {
            _logger = logger;
            var database = cosmosClient.GetDatabase(options.Value.Database);
            _container = database.GetContainer(options.Value.JobsContainer ?? "ingestion-jobs");
        }

        public async Task<IngestionJob> CreateJobAsync(string fileName, string blobUrl, string userId, long fileSizeBytes)
        {
            var job = new IngestionJob
            {
                FileName = fileName,
                BlobUrl = blobUrl,
                UserId = userId,
                FileSizeBytes = fileSizeBytes,
                ContentType = GetContentType(fileName),
                Status = IngestionStatus.Queued
            };

            await _container.CreateItemAsync(job, new PartitionKey(job.UserId));
            _logger.LogInformation("Created ingestion job {JobId} for file {FileName}", job.Id, fileName);
            
            return job;
        }

        public async Task UpdateJobAsync(IngestionJob job)
        {
            await _container.ReplaceItemAsync(job, job.Id, new PartitionKey(job.UserId));
            _logger.LogDebug("Updated job {JobId} with status {Status}", job.Id, job.Status);
        }

        public async Task<IngestionJob?> GetJobAsync(string jobId)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @jobId")
                    .WithParameter("@jobId", jobId);

                var iterator = _container.GetItemQueryIterator<IngestionJob>(query);
                var response = await iterator.ReadNextAsync();
                
                return response.FirstOrDefault();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IngestionJob?> GetJobByFileNameAsync(string fileName)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.fileName = @fileName ORDER BY c.createdAt DESC")
                .WithParameter("@fileName", fileName);

            var iterator = _container.GetItemQueryIterator<IngestionJob>(query);
            var response = await iterator.ReadNextAsync();
            
            return response.FirstOrDefault();
        }

        public async Task<List<IngestionJob>> GetJobsByUserIdAsync(string userId, int limit = 50)
        {
            var query = new QueryDefinition("SELECT TOP @limit * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC")
                .WithParameter("@userId", userId)
                .WithParameter("@limit", limit);

            var iterator = _container.GetItemQueryIterator<IngestionJob>(query);
            var jobs = new List<IngestionJob>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                jobs.AddRange(response);
            }

            return jobs;
        }

        public async Task<List<IngestionJob>> GetJobsByStatusAsync(IngestionStatus status, int limit = 100)
        {
            var query = new QueryDefinition("SELECT TOP @limit * FROM c WHERE c.status = @status ORDER BY c.createdAt DESC")
                .WithParameter("@status", status.ToString())
                .WithParameter("@limit", limit);

            var iterator = _container.GetItemQueryIterator<IngestionJob>(query);
            var jobs = new List<IngestionJob>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                jobs.AddRange(response);
            }

            return jobs;
        }

        private static string GetContentType(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                _ => "application/octet-stream"
            };
        }
    }
}
