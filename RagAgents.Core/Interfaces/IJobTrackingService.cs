using RagAgents.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IJobTrackingService
    {
        Task<IngestionJob> CreateJobAsync(string fileName, string blobUrl, string userId, long fileSizeBytes);
        Task UpdateJobAsync(IngestionJob job);
        Task<IngestionJob?> GetJobAsync(string jobId);
        Task<IngestionJob?> GetJobByFileNameAsync(string fileName);
        Task<List<IngestionJob>> GetJobsByUserIdAsync(string userId, int limit = 50);
        Task<List<IngestionJob>> GetJobsByStatusAsync(IngestionStatus status, int limit = 100);
    }
}
