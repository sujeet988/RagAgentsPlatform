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

    }
}
