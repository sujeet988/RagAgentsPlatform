
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Azure.AI.OpenAI;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Options;

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

            // Register AzureOpenAIClient with proper factory
            builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
                
                // For development: use API key
                if (!string.IsNullOrEmpty(options.Key))
                {
                    return new AzureOpenAIClient(
                        new Uri(options.Endpoint),
                        new AzureKeyCredential(options.Key));
                }
                
                // For production: use Managed Identity (no key needed)
                return new AzureOpenAIClient(
                    new Uri(options.Endpoint),
                    new DefaultAzureCredential());
            });

            // Register Azure Search clients with proper factory
            builder.Services.AddSingleton<SearchClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureSearchAIOptions>>().Value;
                
                // For development: use API key
                if (!string.IsNullOrEmpty(options.Key))
                {
                    return new SearchClient(
                        new Uri(options.Endpoint),
                        options.IndexName,
                        new AzureKeyCredential(options.Key));
                }
                
                // For production: use Managed Identity
                return new SearchClient(
                    new Uri(options.Endpoint),
                    options.IndexName,
                    new DefaultAzureCredential());
            });

            builder.Services.AddSingleton<SearchIndexClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AzureSearchAIOptions>>().Value;
                
                // For development: use API key
                if (!string.IsNullOrEmpty(options.Key))
                {
                    return new SearchIndexClient(
                        new Uri(options.Endpoint),
                        new AzureKeyCredential(options.Key));
                }
                
                // For production: use Managed Identity
                return new SearchIndexClient(
                    new Uri(options.Endpoint),
                    new DefaultAzureCredential());
            });

            // Add services to the container.
            builder.Services.AddScoped<IAzureOpenAIService,AzureOpenAIService>();
            builder.Services.AddScoped<IAzureSearchService,AzureSearchService>();
            builder.Services.AddSingleton<IConversationStoreInMemory, InMemoryConversationStore>();
            builder.Services.AddSingleton<IPromptProvider, PromptProvider>();
            builder.Services.TryAddTransient<IRagService, RagService>();

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
