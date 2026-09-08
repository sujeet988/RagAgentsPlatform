using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Functions.Helper
{
    public static class FunctionUtility
    {
        public static string GetRequiredConfiguration(IConfiguration configuration, string key)
        {
            return configuration[key] ?? throw new InvalidOperationException($"Missing required configuration: {key}");
        }
       public static void ValidateRequiredConfiguration(IConfiguration configuration)
        {
            var requiredKeys = new[]
            {
        "AzureOpenAI:Endpoint",
        "AzureOpenAI:Key",
        "AzureOpenAI:EmbeddingDeployment",
        "AzureSearch:Endpoint",
        "AzureSearch:Key",
        "AzureSearch:IndexName",
        "DocumentAI:Endpoint",
        "DocumentAI:Key"
    };

            var missingKeys = requiredKeys
                .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
                .ToArray();

            if (missingKeys.Length > 0)
            {
                throw new InvalidOperationException($"Missing required configuration: {string.Join(", ", missingKeys)}");
            }
        }

    }
}
