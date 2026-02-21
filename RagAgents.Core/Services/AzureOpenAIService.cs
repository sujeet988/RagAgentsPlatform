using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RagAgents.Core.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly string _embedDeployment;
        private readonly string _chatDeployment;

        // Constructor now accepts the client via DI
        public AzureOpenAIService(
            AzureOpenAIClient client,
            IOptions<AzureOpenAIOptions> options)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
            var cfg = options.Value;
            _embedDeployment = cfg.EmbeddingDeployment;
            _chatDeployment = cfg.ChatDeployment;
        }
        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            try
            {
                var embeddingClient = _client.GetEmbeddingClient(_embedDeployment);

                var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(text);

                // Access the embedding from the Value property, then call ToFloats()
                return embeddingResult.Value.ToFloats().ToArray();

            }
            catch (Exception)
            {
                // Log the error and rethrow (preserves stack trace)
                throw;
            }
        }

        public async Task<string> GenerateAnswerAsync(string prompt)
        {
            // Get chat client for your Azure OpenAI deployment
            var chatClient = _client.GetChatClient(_chatDeployment);

            // Prepare chat messages
            var messages = new List<ChatMessage>
            {
            new SystemChatMessage("You are an enterprise assistant. Answer only using context provided."),
            new UserChatMessage(prompt)
            };

            // Call the Azure OpenAI chat completion API
            var response = await chatClient.CompleteChatAsync(messages);

            // Read the generated text
            return response.Value.Content.LastOrDefault()?.Text ?? string.Empty;
        }

        public async Task StreamChatAsync(string systemPrompt, string userPrompt, Func<string, Task> onToken)
        {
            var chatClient = _client.GetChatClient(_chatDeployment);

            var messages = new ChatMessage[]
            {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
            };

            await foreach (var update in chatClient.CompleteChatStreamingAsync(messages))
            {
                foreach (var content in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(content.Text))
                        await onToken(content.Text);
                }
            }
        }
    }
}
