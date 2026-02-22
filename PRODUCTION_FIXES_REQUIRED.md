# Production Readiness Fixes

## Priority 1: Security (IMMEDIATE)

### 1. Remove API Keys from appsettings.json
```bash
# Use Azure Key Vault references instead
"AzureOpenAI": {
  "Endpoint": "https://your-resource.openai.azure.com/",
  "Key": "@Microsoft.KeyVault(SecretUri=https://kv-ragagents-prod.vault.azure.net/secrets/OpenAI-Key/)"
}

# Or remove Key entirely and use Managed Identity (already implemented)
"AzureOpenAI": {
  "Endpoint": "https://your-resource.openai.azure.com/"
  # No Key property - uses DefaultAzureCredential
}
```

### 2. Fix CORS for Production
```csharp
// appsettings.Production.json
"Cors": {
  "AllowedOrigins": [
    "https://yourdomain.com",
    "https://app.yourdomain.com"
  ]
}

// Program.cs
options.AddPolicy("AllowFrontend", policy =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    policy
        .WithOrigins(allowedOrigins!)
        .AllowAnyHeader()
        .WithMethods("GET", "POST", "PUT", "DELETE");
});
```

### 3. Fix JSON Configuration
Remove all `//` comments from appsettings.Versioning.json or use JSONC parser.

---

## Priority 2: Reliability (HIGH)

### 4. Add Global Exception Handler
```csharp
// Program.cs after var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Add Error Controller
[ApiController]
[Route("[controller]")]
public class ErrorController : ControllerBase
{
    [Route("/error")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult HandleError() =>
        Problem();
}
```

### 5. Add Health Checks
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddAzureCosmosDB(cosmosEndpoint, credential)
    .AddAzureSearch(searchEndpoint, credential)
    .AddCheck<OpenAIHealthCheck>("openai");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});
```

### 6. Add Rate Limiting (ASP.NET Core 7+)
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
    
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 1000;
        opt.TokensPerPeriod = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

// On controllers
[EnableRateLimiting("fixed")]
public class ChatController : ControllerBase { }
```

### 7. Fix Exception Handling
Replace all `throw ex;` with `throw;` or proper logging:
```csharp
catch (Exception)
{
    _logger.LogError(ex, "Error indexing document");
    throw;  // ✅ Preserves stack trace
}
```

### 8. Add Circuit Breaker (Polly)
```csharp
builder.Services.AddHttpClient<IAzureOpenAIService, AzureOpenAIService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
```

---

## Priority 3: Testing (HIGH)

### 9. Add Unit Tests Project
```bash
dotnet new xunit -n RagAgents.Tests
dotnet add RagAgents.Tests reference RagAgents.Core
dotnet add RagAgents.Tests package Moq
dotnet add RagAgents.Tests package FluentAssertions
dotnet sln add RagAgents.Tests
```

Example test:
```csharp
public class AzureOpenAIServiceTests
{
    [Fact]
    public async Task CreateEmbeddingAsync_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var mockClient = new Mock<AzureOpenAIClient>();
        // ... setup mocks
        var service = new AzureOpenAIService(mockClient.Object, options);
        
        // Act
        var result = await service.CreateEmbeddingAsync("test");
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1536);
    }
}
```

---

## Priority 4: Configuration (MEDIUM)

### 10. Environment-Specific Config
```
appsettings.json                  # Base
appsettings.Development.json      # Dev overrides
appsettings.Staging.json          # Staging overrides
appsettings.Production.json       # Prod overrides (NO SECRETS!)
```

### 11. Configuration Validation
```csharp
builder.Services.AddOptions<AzureOpenAIOptions>()
    .Bind(builder.Configuration.GetSection("AzureOpenAI"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

public class AzureOpenAIOptions
{
    [Required, Url]
    public string Endpoint { get; set; } = default!;
    
    [Required]
    public string ChatDeployment { get; set; } = default!;
}
```

---

## Additional Improvements

### 12. Add Request Validation
```csharp
// Install: Microsoft.AspNetCore.Mvc.DataAnnotations
public class QuestionRequest
{
    [Required(ErrorMessage = "Question is required")]
    [StringLength(1000, MinimumLength = 1)]
    public string Question { get; set; } = default!;
    
    [Range(1, 10)]
    public int MaxResults { get; set; } = 5;
}
```

### 13. Add Response Caching
```csharp
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache();

app.UseResponseCaching();
app.UseOutputCache();

// On endpoints
[OutputCache(Duration = 300)]
public async Task<IActionResult> GetStatistics() { }
```

### 14. Add API Versioning
```csharp
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
});

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ChatController : ControllerBase { }
```

### 15. Add Distributed Cache
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Use in services
private readonly IDistributedCache _cache;

public async Task<string> GetCachedEmbedding(string text)
{
    var cacheKey = $"embedding:{text.GetHashCode()}";
    var cached = await _cache.GetStringAsync(cacheKey);
    if (cached != null) return cached;
    
    var embedding = await _openAI.CreateEmbeddingAsync(text);
    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(embedding),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) });
    
    return embedding;
}
```

---

## Deployment Checklist

Before production deploy:
- [ ] All API keys removed from config
- [ ] Key Vault configured with Managed Identity
- [ ] Health checks implemented and tested
- [ ] Rate limiting configured
- [ ] Global exception handler added
- [ ] CORS restricted to specific domains
- [ ] Unit tests with >70% coverage
- [ ] Load testing completed
- [ ] Security scan passed
- [ ] Monitoring dashboards created
- [ ] Alerts configured
- [ ] Runbooks documented
- [ ] Backup tested
- [ ] Disaster recovery plan reviewed
