using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Models
{
    public class AzureOpenAIOptions
    {
        public string Endpoint { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string EmbeddingDeployment { get; set; } = default!;
        public string ChatDeployment { get; set; } = default!;
        public AzureCosmosOptions AzureCosmos { get; set; } = new();
        
        // Model Versioning
        public string ChatModelVersion { get; set; } = "gpt-4-0613";
        public string EmbeddingModelVersion { get; set; } = "text-embedding-3-large";
        public ModelVersioningOptions Versioning { get; set; } = new();
    }
    
    public class ModelVersioningOptions
    {
        public bool EnableVersionTracking { get; set; } = true;
        public bool LogModelMetrics { get; set; } = true;
        public Dictionary<string, ModelVersion> AvailableVersions { get; set; } = new();
    }
    
    public class ModelVersion
    {
        public string Name { get; set; } = default!;
        public string DeploymentName { get; set; } = default!;
        public string Version { get; set; } = default!;
        public int MaxTokens { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ActivatedDate { get; set; }
    }
    public class AzureSearchAIOptions
    {
        public string Endpoint { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string IndexName { get; set; } = default!;
    }

    public class AzureCosmosOptions
    {
        public string Endpoint { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string Database { get; set; } = default!;
        public string Container { get; set; } = "conversations";
        public string JobsContainer { get; set; } = "ingestion-jobs";
    }
}
