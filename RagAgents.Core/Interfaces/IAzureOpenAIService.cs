using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IAzureOpenAIService
    {
        // Replace the CreateEmbeddingAsync method with the following implementation
        Task<float[]> CreateEmbeddingAsync(string text);
        Task StreamChatAsync(string systemPrompt,string userPrompt,Func<string, Task> onToken);

        // 🔹 Generate answer using chat deployment
        Task<string> GenerateAnswerAsync(string prompt);
        
        // Model versioning
        string GetChatModelVersion();
        string GetEmbeddingModelVersion();
    }
}
