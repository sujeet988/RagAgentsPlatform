using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// get all config values
var config = builder.Configuration;
var result = config["DocumentAI:Endpoint"];

//ADD YOUR SERVICES HERE 
builder.Services.AddSingleton<IPdfIngestService, PdfIngestService>();
builder.Services.AddSingleton<IRagService, RagService>();

// Register DocumentAnalysisClient for Form Recognizer
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    return new DocumentAnalysisClient(
        new Uri(config["DocumentAI:Endpoint"]),
        new AzureKeyCredential(config["DocumentAI:Key"]));
});

builder.Build().Run();
