using RagAgents.Core.Models;

namespace RagAgents.Core.Interfaces
{
    /// <summary>
    /// Service for managing model and prompt versions
    /// </summary>
    public interface IVersionTrackingService
    {
        /// <summary>
        /// Track usage of a specific model version
        /// </summary>
        Task TrackModelUsageAsync(ModelUsageRecord record);
        
        /// <summary>
        /// Track usage of a specific prompt version
        /// </summary>
        Task TrackPromptUsageAsync(PromptUsageRecord record);
        
        /// <summary>
        /// Get model usage statistics
        /// </summary>
        Task<ModelUsageStats> GetModelUsageStatsAsync(string modelVersion, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Get prompt usage statistics
        /// </summary>
        Task<PromptUsageStats> GetPromptUsageStatsAsync(string promptVersion, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Switch to a different model version
        /// </summary>
        Task<bool> SwitchModelVersionAsync(string deploymentName, string newVersion);
        
        /// <summary>
        /// Switch to a different prompt version
        /// </summary>
        Task<bool> SwitchPromptVersionAsync(string newVersion);
    }

    public class ModelUsageRecord
    {
        public string ModelVersion { get; set; } = default!;
        public string DeploymentName { get; set; } = default!;
        public string OperationType { get; set; } = default!; // "chat" or "embedding"
        public int TokensUsed { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public double ResponseTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = default!;
    }

    public class PromptUsageRecord
    {
        public string PromptVersion { get; set; } = default!;
        public string PromptType { get; set; } = default!; // "SimpleRag", "RagWithHistory", etc.
        public string Question { get; set; } = default!;
        public string ModelVersion { get; set; } = default!;
        public int ResponseLength { get; set; }
        public double ResponseTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = default!;
    }

    public class ModelUsageStats
    {
        public string ModelVersion { get; set; } = default!;
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public long TotalTokensUsed { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests : 0;
    }

    public class PromptUsageStats
    {
        public string PromptVersion { get; set; } = default!;
        public int TotalUsages { get; set; }
        public int SuccessfulUsages { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double AverageResponseLength { get; set; }
        public Dictionary<string, int> UsageByType { get; set; } = new();
    }
}
