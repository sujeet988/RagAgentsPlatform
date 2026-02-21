using System;

namespace RagAgents.Core.Models
{
    public class IngestionJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = default!;
        public string BlobUrl { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public IngestionStatus Status { get; set; } = IngestionStatus.Queued;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Progress tracking
        public int TotalChunks { get; set; }
        public int ProcessedChunks { get; set; }
        public int FailedChunks { get; set; }
        
        // Telemetry
        public long ExtractionTimeMs { get; set; }
        public long ChunkingTimeMs { get; set; }
        public long EmbeddingTimeMs { get; set; }
        public long IndexingTimeMs { get; set; }
        public long TotalTimeMs { get; set; }
        
        // Error tracking
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        
        // Metadata
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = default!;
    }

    public enum IngestionStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        PartiallyCompleted
    }
}
