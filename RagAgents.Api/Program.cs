
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace RagAgents.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // add http client
            builder.Services.AddHttpClient();

            #region  Add Authentication and authorization
            // Configure Microsoft Identity Web API (uses AzureAd section)
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RagUser", policy => policy.RequireRole("RagUser"));
                options.AddPolicy("RagAdmin", policy => policy.RequireRole("RagAdmin"));
            });
            #endregion

            # region Load Configuration
            builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
            // Use the same section name used in local.settings.json / Functions project
            builder.Services.Configure<AzureSearchAIOptions>(builder.Configuration.GetSection("AzureSearchAI"));
            #endregion
            #region Add/register services to the container.
            builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
            builder.Services.AddSingleton<IAzureSearchService, AzureSearchService>();
            builder.Services.AddSingleton<IConversationStoreInMemory, InMemoryConversationStore>();
            // RagService can be scoped per-request
            builder.Services.AddScoped<IRagService, RagService>();
            builder.Services.AddScoped<RagAgents.Api.Services.IChatService, RagAgents.Api.Services.ChatService>();

            // Register agent and tools
            builder.Services.AddScoped<RagAgents.Core.Agents.IAgent, RagAgents.Core.Agents.AgentService>();
            builder.Services.AddScoped<RagAgents.Core.Agents.ITool, RagAgents.Core.Agents.SearchTool>(sp =>
            {
                var search = sp.GetRequiredService<IAzureSearchService>();
                var openAI = sp.GetRequiredService<IAzureOpenAIService>();
                return new RagAgents.Core.Agents.SearchTool(search, openAI);
            });
            builder.Services.AddScoped<RagAgents.Core.Agents.ITool, RagAgents.Core.Agents.PdfIngestTool>(sp =>
            {
                // Resolve IPdfIngestService if available, otherwise use NoOpPdfIngestService
                var ingest = sp.GetService<IDocumentIngestService>() ?? new RagAgents.Core.Services.NoOpPdfIngestService();
                return new RagAgents.Core.Agents.PdfIngestTool(ingest);
            });
            #endregion

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
