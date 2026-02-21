using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;

namespace RagAgents.Core.Extensions
{
    public static class AzureClientExtensions
    {
        /// <summary>
        /// Registers Azure OpenAI and Azure Search clients with proper authentication
        /// (API Key for Development, Managed Identity for Production)
        /// </summary>
        public static IServiceCollection AddAzureClients(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var isDevelopment = environment.IsDevelopment();

            // Register AzureOpenAIClient
            services.AddSingleton<AzureOpenAIClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
                var endpoint = new Uri(options.Endpoint);

                if (isDevelopment && !string.IsNullOrEmpty(options.Key))
                {
                    // Development: Use API Key
                    return new AzureOpenAIClient(endpoint, new AzureKeyCredential(options.Key));
                }

                // Production: Use Managed Identity
                return new AzureOpenAIClient(endpoint, new DefaultAzureCredential());
            });

            // Register SearchClient
            services.AddSingleton<SearchClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureSearchAIOptions>>().Value;
                var endpoint = new Uri(options.Endpoint);

                if (isDevelopment && !string.IsNullOrEmpty(options.Key))
                {
                    // Development: Use API Key
                    return new SearchClient(endpoint, options.IndexName, new AzureKeyCredential(options.Key));
                }

                // Production: Use Managed Identity
                return new SearchClient(endpoint, options.IndexName, new DefaultAzureCredential());
            });

            // Register SearchIndexClient
            services.AddSingleton<SearchIndexClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureSearchAIOptions>>().Value;
                var endpoint = new Uri(options.Endpoint);

                if (isDevelopment && !string.IsNullOrEmpty(options.Key))
                {
                    // Development: Use API Key
                    return new SearchIndexClient(endpoint, new AzureKeyCredential(options.Key));
                }

                // Production: Use Managed Identity
                return new SearchIndexClient(endpoint, new DefaultAzureCredential());
            });

            return services;
        }

        /// <summary>
        /// Registers all RAG-related services with proper lifetimes
        /// </summary>
        public static IServiceCollection AddRagServices(this IServiceCollection services)
        {
            // Register services with Scoped lifetime for better resource management
            services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
            services.AddScoped<IAzureSearchService, AzureSearchService>();
            
            // Conversation store and prompt provider as Singleton (stateless)
            services.AddSingleton<IConversationStoreInMemory, InMemoryConversationStore>();
            services.AddSingleton<IPromptProvider, PromptProvider>();
            
            // RAG service as Transient (created per request)
            services.AddTransient<IRagService, RagService>();

            return services;
        }
    }
}
