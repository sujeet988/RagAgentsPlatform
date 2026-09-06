using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Interfaces
{
    public interface IOpenAIEmbeddingService
    {
        // Replace the CreateEmbeddingAsync method with the following implementation
        Task<float[]> CreateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<float[]>> CreateEmbeddingsBatchAsync(IReadOnlyList<string> texts,CancellationToken cancellationToken = default);
        Task StreamChatAsync(string systemPrompt,string userPrompt,Func<string, Task> onToken, CancellationToken cancellationToken = default);

        // 🔹 Generate answer using chat deployment
        Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default);


    }
}
