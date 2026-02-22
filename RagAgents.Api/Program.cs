
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;
using RagAgents.Core.Extensions;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace RagAgents.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // add http clinet
            builder.Services.AddHttpClient();

            // Add Authentication and authrization
            builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(
             builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RagUser", policy =>
                    policy.RequireRole("RagUser"));

                options.AddPolicy("RagAdmin", policy =>
                    policy.RequireRole("RagAdmin"));
            });

            // Load Configuration
            builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
            builder.Services.Configure<AzureSearchAIOptions>(builder.Configuration.GetSection("AzureSearchAI"));
            builder.Services.Configure<PromptOptions>(builder.Configuration.GetSection("PromptOptions"));

            // Configure OpenTelemetry
            var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "RagAgents.Api";
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = builder.Environment.EnvironmentName
                    }))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("RagAgents.*")
                    .AddOtlpExporter(options =>
                    {
                        if (!string.IsNullOrEmpty(otlpEndpoint))
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }
                    }))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter("RagAgents.*")
                    .AddOtlpExporter(options =>
                    {
                        if (!string.IsNullOrEmpty(otlpEndpoint))
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }
                    }));

            // Register Azure clients (OpenAI, Search) with environment-based authentication
            builder.Services.AddAzureClients(builder.Configuration, builder.Environment);

            // Register RAG-related services
            builder.Services.AddRagServices();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            //CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //MUST be before authorization & controllers
            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
