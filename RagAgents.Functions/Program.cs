using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RagAgents.Core.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure options using standard binding
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<AzureSearchAIOptions>(
    builder.Configuration.GetSection("AzureSearch"));
builder.Services.Configure<DocumentAIOptions>(
    builder.Configuration.GetSection("DocumentAI"));

// Register Azure clients using the extension method
builder.Services.AddAzureClients(builder.Configuration, builder.Environment);

// Register DocumentAnalysisClient for Form Recognizer
builder.Services.AddSingleton<DocumentAnalysisClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<DocumentAIOptions>>().Value;
    var endpoint = new Uri(options.Endpoint);

    if (builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(options.Key))
    {
        // Development: Use API Key
        return new DocumentAnalysisClient(endpoint, new AzureKeyCredential(options.Key));
    }

    // Production: Use Managed Identity
    return new DocumentAnalysisClient(endpoint, new DefaultAzureCredential());
});

// Register RAG services
builder.Services.AddRagServices();

// Register PdfIngestService
builder.Services.AddScoped<IPdfIngestService, PdfIngestService>();

builder.Build().Run();
