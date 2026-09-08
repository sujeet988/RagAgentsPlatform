using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;
using RagAgents.Functions.Helper;
using RagAgents.Functions.Services;

namespace RagAgents.Functions.IoC
{
    public static class IocModuleExtensions
    {
        public static IServiceCollection AddFunctionAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            FunctionUtility.ValidateRequiredConfiguration(configuration);

            services.Configure<AzureOpenAIOptions>(options =>
            {
                options.Endpoint = FunctionUtility.GetRequiredConfiguration(configuration, "AzureOpenAI:Endpoint");
                options.Key = FunctionUtility.GetRequiredConfiguration(configuration, "AzureOpenAI:Key");
                options.EmbeddingDeployment = FunctionUtility.GetRequiredConfiguration(configuration, "AzureOpenAI:EmbeddingDeployment");
                options.ChatDeployment = configuration["AzureOpenAI:ChatDeployment"] ?? string.Empty;
            });

            services.Configure<AzureSearchAIOptions>(options =>
            {
                options.Endpoint = FunctionUtility.GetRequiredConfiguration(configuration, "AzureSearch:Endpoint");
                options.Key = FunctionUtility.GetRequiredConfiguration(configuration, "AzureSearch:Key");
                options.IndexName = FunctionUtility.GetRequiredConfiguration(configuration, "AzureSearch:IndexName");
                options.VectorDimensions = configuration.GetValue("AzureSearch:VectorDimensions", 3072);
            });

            services.Configure<IngestionOptions>(configuration.GetSection("Ingestion"));

            services.AddSingleton<IOpenAIEmbeddingService, AzureOpenAIEmbeddingService>();
            services.AddSingleton<ISearchIndexer, AzureSearchSearchIndexerService>();
            services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
            services.AddScoped<IDocumentIngestService, DocumentIngestService>();
            services.AddScoped<IFunctionIngestService, FunctionIngestService>();

            services.AddSingleton(_ => new DocumentAnalysisClient(
                new Uri(FunctionUtility.GetRequiredConfiguration(configuration, "DocumentAI:Endpoint")),
                new AzureKeyCredential(FunctionUtility.GetRequiredConfiguration(configuration, "DocumentAI:Key"))));

            return services;
        }
    }

}
