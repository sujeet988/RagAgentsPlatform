using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

ValidateRequiredConfiguration(builder.Configuration);

builder.Services.Configure<AzureOpenAIOptions>(options =>
{
    options.Endpoint = GetRequiredConfiguration(builder.Configuration, "AzureOpenAI:Endpoint");
    options.Key = GetRequiredConfiguration(builder.Configuration, "AzureOpenAI:Key");
    options.EmbeddingDeployment = GetRequiredConfiguration(builder.Configuration, "AzureOpenAI:EmbeddingDeployment");
    options.ChatDeployment = builder.Configuration["AzureOpenAI:ChatDeployment"] ?? string.Empty;
});
builder.Services.Configure<AzureSearchAIOptions>(options =>
{
    options.Endpoint = GetRequiredConfiguration(builder.Configuration, "AzureSearch:Endpoint");
    options.Key = GetRequiredConfiguration(builder.Configuration, "AzureSearch:Key");
    options.IndexName = GetRequiredConfiguration(builder.Configuration, "AzureSearch:IndexName");
    options.VectorDimensions = builder.Configuration.GetValue("AzureSearch:VectorDimensions", 3072);
});
builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Ingestion"));

var config = builder.Configuration;

builder.Services.AddSingleton<IOpenAIEmbeddingService, AzureOpenAIEmbeddingService>();
builder.Services.AddSingleton<ISearchIndexer, AzureSearchSearchIndexerService>();
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddScoped<IDocumentIngestService, DocumentIngestService>();
builder.Services.AddScoped<RagAgents.Functions.Services.IFunctionIngestService, RagAgents.Functions.Services.FunctionIngestService>();

builder.Services.AddSingleton(sp =>
{
    return new DocumentAnalysisClient(
    new Uri(GetRequiredConfiguration(config, "DocumentAI:Endpoint")),
    new AzureKeyCredential(GetRequiredConfiguration(config, "DocumentAI:Key")));
});

builder.Build().Run();

static void ValidateRequiredConfiguration(IConfiguration configuration)
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

static string GetRequiredConfiguration(IConfiguration configuration, string key)
{
    return configuration[key] ?? throw new InvalidOperationException($"Missing required configuration: {key}");
}
