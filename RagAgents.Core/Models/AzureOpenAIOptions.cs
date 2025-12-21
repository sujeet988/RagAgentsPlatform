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
    }
    public class AzureSearchAIOptions
    {
        public string Endpoint { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string IndexName { get; set; } = default!;

    }
}
