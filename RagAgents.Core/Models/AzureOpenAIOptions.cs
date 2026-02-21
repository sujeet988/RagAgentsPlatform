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
