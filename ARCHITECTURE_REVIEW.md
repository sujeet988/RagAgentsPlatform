# RagAgents Architecture Review & Improvement Plan

## 📊 Current Architecture Assessment

### Project Structure
- **RagAgents.Api**: ASP.NET Core Web API (presentation layer)
- **RagAgents.Core**: Business logic and domain models
- **RagAgents.Functions**: Azure Functions for PDF ingestion

### Architecture Pattern
✅ 3-tier architecture with API → Core → Infrastructure
✅ Dependency injection with interface abstractions
✅ Azure AD authentication with role-based authorization

---

## 🚨 CRITICAL ISSUES (Must Fix Before Production)

### 1. Security Vulnerabilities

#### **Exposed Secrets** (Priority: 🔴 CRITICAL)
**Problem:** API keys, connection strings hardcoded in `appsettings.json`

**Fix:** Use Azure Key Vault or User Secrets

```bash
# Move to User Secrets (Development)
dotnet user-secrets init --project RagAgents.Api
dotnet user-secrets set "AzureOpenAI:Key" "your-key" --project RagAgents.Api
dotnet user-secrets set "AzureSearchAI:Key" "your-key" --project RagAgents.Api
```

**Production Solution:**
```csharp
// Program.cs
var keyVaultEndpoint = new Uri(builder.Configuration["KeyVaultEndpoint"]!);
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
```

```json
// appsettings.Production.json
{
  "KeyVaultEndpoint": "https://your-keyvault.vault.azure.net/",
  "AzureOpenAI": {
    "Endpoint": "https://...",
    "Key": "",  // ← Empty, loaded from Key Vault
    "EmbeddingDeployment": "text-embedding-3-large",
    "ChatDeployment": "gpt-4"
  }
}
```

#### **CORS Misconfiguration** (Priority: 🔴 CRITICAL)
**Problem:** `AllowAnyOrigin()` allows any website to call your API

**Fix:**
```csharp
// Program.cs - Update CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://yourdomain.com",
                "https://app.yourdomain.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // If using cookies/auth
    });
});
```

---

### 2. Error Handling & Resilience

#### **Issue:** No Global Exception Handler
```csharp
// Create Middleware/GlobalExceptionHandler.cs
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred");

        var problemDetails = exception switch
        {
            ValidationException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = exception.Message
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An error occurred while processing your request"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

// Register in Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

#### **Issue:** Services throw raw exceptions with `throw ex;`
**Fix:** Replace all `throw ex;` with `throw;` (preserves stack trace)

```csharp
// WRONG ❌
catch (Exception ex)
{
    throw ex;  // Loses original stack trace
}

// CORRECT ✅
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to create embedding");
    throw;  // Preserves stack trace
}
```

#### **Add Retry Policies with Polly**
```bash
dotnet add package Microsoft.Extensions.Http.Polly
```

```csharp
// Program.cs
builder.Services.AddHttpClient<IAzureOpenAIService, AzureOpenAIService>()
    .AddTransientHttpErrorPolicy(policy => 
        policy.WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddTransientHttpErrorPolicy(policy => 
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

---

### 3. Observability & Monitoring

#### **Add Application Insights**
```bash
dotnet add package Microsoft.ApplicationInsights.AspNetCore
```

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// appsettings.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."
  }
}
```

#### **Add Health Checks**
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddAzureSearch(sp => sp.GetRequiredService<IAzureSearchService>())
    .AddAzureOpenAI(sp => sp.GetRequiredService<IAzureOpenAIService>())
    .AddCosmosDb(cosmosEndpoint, cosmosKey, "rag-chat-db");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

#### **Structured Logging with Serilog**
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.ApplicationInsights
```

```csharp
// Program.cs
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();
```

---

### 4. Performance Optimizations

#### **Add Response Caching**
```csharp
// Program.cs
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// ChatController.cs
[HttpPost("ask")]
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "question" })]
public async Task<IActionResult> Ask([FromBody] QuestionRequest request)
{
    // ... existing code
}
```

#### **Add Request Compression**
```csharp
// Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

app.UseResponseCompression();
```

#### **Semantic Caching for RAG**
```csharp
public interface ISemanticCache
{
    Task<string?> GetAsync(float[] embedding, float threshold = 0.95f);
    Task SetAsync(float[] embedding, string answer, TimeSpan? expiry = null);
}

// Implementation using Redis with vector similarity
public class RedisSemanticCache : ISemanticCache
{
    // ... implementation
}
```

---

### 5. Service Lifetime Issues

**Problem:** Singletons may cause issues with HttpClient

**Fix:**
```csharp
// Change from Singleton to Scoped
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();

// OR use IHttpClientFactory
builder.Services.AddHttpClient<IAzureOpenAIService, AzureOpenAIService>();
```

---

## ✨ Prompt Configuration (Implemented)

### New Files Created:
1. ✅ `RagAgents.Core/Models/PromptOptions.cs`
2. ✅ `RagAgents.Core/Interfaces/IPromptProvider.cs`
3. ✅ `RagAgents.Core/Services/PromptProvider.cs`
4. ✅ `RagAgents.Api/appsettings.prompts.json`

### Usage:
```json
// appsettings.prompts.json
{
  "PromptOptions": {
    "SystemPrompt": "You are an enterprise assistant.",
    "RagPrompts": {
      "SimpleRag": "Answer using ONLY the context...",
      "RagWithHistory": "Conversation History:\n{history}..."
    },
    "CustomPrompts": {
      "summarize": "Summarize in {max_words} words:\n{text}"
    }
  }
}
```

```csharp
// RagService.cs (Updated)
private readonly IPromptProvider _promptProvider;

public async Task<string> AskAsync(string question)
{
    var context = ...;
    var prompt = _promptProvider.GetSimpleRagPrompt(context, question);
    return await _openAI.GenerateAnswerAsync(prompt);
}

// For custom prompts
var summarizePrompt = _promptProvider.GetCustomPrompt("summarize", new Dictionary<string, string>
{
    ["max_words"] = "100",
    ["text"] = documentText
});
```

**Benefits:**
- ✅ Change prompts without redeployment
- ✅ A/B test different prompts via configuration
- ✅ Environment-specific prompts (dev vs prod)
- ✅ Version control prompt templates separately

---

## 🏗️ Recommended Architecture Improvements

### Create Infrastructure Layer
```
RagAgents.Infrastructure/
├── Persistence/
│   ├── CosmosRepository.cs          # Generic repository
│   └── ConversationRepository.cs    # Specific implementation
├── AI/
│   ├── AzureOpenAIProvider.cs      # Move from Core
│   └── AzureSearchProvider.cs      # Move from Core
└── Configuration/
    └── KeyVaultConfigProvider.cs
```

### Add Validation with FluentValidation
```bash
dotnet add package FluentValidation.AspNetCore
```

```csharp
public class QuestionRequestValidator : AbstractValidator<QuestionRequest>
{
    public QuestionRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required")
            .MaximumLength(1000).WithMessage("Question too long");
            
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required");
    }
}

// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<QuestionRequestValidator>();
```

### Add API Versioning
```bash
dotnet add package Asp.Versioning.Mvc
```

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// ChatController.cs
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ChatController : ControllerBase
```

### Add Rate Limiting (.NET 8)
```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

// ChatController.cs
[EnableRateLimiting("fixed")]
public class ChatController : ControllerBase
```

---

## 📋 Production Readiness Checklist

### Security
- [ ] Move secrets to Azure Key Vault
- [ ] Fix CORS to specific origins
- [ ] Add rate limiting
- [ ] Enable HTTPS only
- [ ] Add API key authentication (optional)
- [ ] Implement IP whitelisting (if needed)

### Reliability
- [ ] Add global exception handler
- [ ] Fix `throw ex;` to preserve stack traces
- [ ] Add retry policies with Polly
- [ ] Add circuit breakers
- [ ] Implement health checks

### Observability
- [ ] Add Application Insights
- [ ] Implement structured logging (Serilog)
- [ ] Add distributed tracing
- [ ] Create dashboard for monitoring
- [ ] Set up alerts for errors/latency

### Performance
- [ ] Add response caching
- [ ] Implement semantic caching for RAG
- [ ] Add compression (Gzip/Brotli)
- [ ] Use `IHttpClientFactory`
- [ ] Add pagination for history endpoint

### Testing
- [ ] Add unit tests for services
- [ ] Add integration tests for API
- [ ] Add load tests
- [ ] Test authentication flow end-to-end

### Documentation
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Deployment guide
- [ ] Configuration guide
- [ ] Troubleshooting guide

### DevOps
- [ ] CI/CD pipeline
- [ ] Infrastructure as Code (Bicep/Terraform)
- [ ] Environment-specific configs
- [ ] Blue-green deployment strategy

---

## 🎯 Quick Wins (Do These First)

1. **Move secrets to User Secrets** (5 min)
2. **Fix CORS** (2 min)
3. **Replace `throw ex;` with `throw;`** (5 min)
4. **Add global exception handler** (15 min)
5. **Add health checks** (10 min)
6. **Change service lifetimes to Scoped** (2 min)
7. **Add Application Insights** (10 min)

**Total time: ~50 minutes for critical fixes**

---

## 📚 Additional Resources

- [Azure Key Vault Configuration Provider](https://learn.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)
- [.NET Exception Handling Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [Polly Resilience](https://github.com/App-vNext/Polly)
- [Application Insights for .NET](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core)
- [Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

---

**Status:** ❌ NOT production-ready (critical security issues)  
**After implementing fixes:** ✅ Production-ready
