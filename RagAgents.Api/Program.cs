
using Microsoft.Extensions.DependencyInjection.Extensions;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;
using RagAgents.Core.Services;

namespace RagAgents.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load Configuration
            builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
            builder.Services.Configure<AzureSearchAIOptions>(builder.Configuration.GetSection("AzureSearchAI"));

            // Add services to the container.
            builder.Services.AddSingleton<IAzureOpenAIService,AzureOpenAIService>();
            builder.Services.AddSingleton<IAzureSearchService,AzureSearchService>();
            builder.Services.AddSingleton<IConversationStore,ConversationStore>();
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

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
