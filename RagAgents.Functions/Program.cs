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

// mannaly  bind config value :
builder.Services.Configure<AzureOpenAIOptions>(options =>
{
    options.Endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
    options.Key = builder.Configuration["AzureOpenAI:Key"];
    options.EmbeddingDeployment = builder.Configuration["AzureOpenAI:EmbeddingDeployment"];
    options.ChatDeployment = builder.Configuration["AzureOpenAI:ChatDeployment"];
});
builder.Services.Configure<AzureSearchAIOptions>(options =>
{
    options.Endpoint = builder.Configuration["AzureSearch:Endpoint"];
    options.Key = builder.Configuration["AzureSearch:Key"];
    options.IndexName = builder.Configuration["AzureSearch:IndexName"];
});

// get all config values
var config = builder.Configuration;
var result = config["DocumentAI:Endpoint"];

//ADD YOUR SERVICES HERE 
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddSingleton<IAzureSearchService, AzureSearchService>();
// Register PdfIngestService with the DocumentAnalysisClient injected
builder.Services.AddSingleton<IPdfIngestService>(sp =>
{
    var docClient = sp.GetRequiredService<DocumentAnalysisClient>();
    var openAI = sp.GetRequiredService<IAzureOpenAIService>();
    var search = sp.GetRequiredService<IAzureSearchService>();

    return new PdfIngestService(docClient, openAI, search);
});

// Register function-level facade
builder.Services.AddScoped<RagAgents.Functions.Services.IFunctionIngestService, RagAgents.Functions.Services.FunctionIngestService>();


// Register DocumentAnalysisClient for Form Recognizer
builder.Services.AddSingleton(sp =>
{
    return new DocumentAnalysisClient(
        new Uri(config["DocumentAI:Endpoint"]),
        new AzureKeyCredential(config["DocumentAI:Key"]));
});

builder.Build().Run();
