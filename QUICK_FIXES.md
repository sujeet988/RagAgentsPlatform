# Quick Production Fix Guide

## 🚨 CRITICAL: Fix These NOW (Before Any Deployment)

### 1. Remove Secrets from appsettings.json (5 minutes)

**Step 1:** Initialize User Secrets
```powershell
cd RagAgents.Api
dotnet user-secrets init
```

**Step 2:** Move secrets to User Secrets
```powershell
# Azure OpenAI
dotnet user-secrets set "AzureOpenAI:Key" "<YOUR_AZURE_OPENAI_KEY>"

# Azure Search
dotnet user-secrets set "AzureSearchAI:Key" "<YOUR_AZURE_SEARCH_KEY>"

# Cosmos (if using)
dotnet user-secrets set "AzureCosmos:Key" "<YOUR_COSMOS_KEY>"
```

**Step 3:** Update appsettings.json (remove keys)
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Key": "",
    "EmbeddingDeployment": "text-embedding-3-large",
    "ChatDeployment": "gpt-4"
  },
  "AzureSearchAI": {
    "Endpoint": "https://your-search.search.windows.net",
    "Key": "",
    "IndexName": "documents-index"
  }
}
```

**Step 4:** Add secrets.json to .gitignore (if not already)
```
**/secrets.json
appsettings.*.json
!appsettings.json
```

---

### 2. Fix CORS (2 minutes)

Update Program.cs:
```csharp
// Replace this:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()  // ❌ INSECURE
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// With this:
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
        ?? new[] { "http://localhost:3000" };
    
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)  // ✅ SECURE
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

Add to appsettings.json:
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ]
}
```

Add to appsettings.Production.json:
```json
{
  "AllowedOrigins": [
    "https://yourdomain.com",
    "https://app.yourdomain.com"
  ]
}
```

---

### 3. Fix Exception Handling (5 minutes)

Replace all instances of `throw ex;` with `throw;`:

**Before (WRONG):**
```csharp
catch (Exception ex)
{
    throw ex;  // ❌ Loses stack trace
}
```

**After (CORRECT):**
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed: {ErrorMessage}", ex.Message);
    throw;  // ✅ Preserves stack trace
}
```

**Files to fix:**
- RagAgents.Core/Services/AzureOpenAIService.cs (line 37)
- RagAgents.Core/Services/AzureSearchService.cs (line 47)

---

### 4. Add Global Exception Handler (10 minutes)

Create `RagAgents.Api/Middleware/GlobalExceptionHandler.cs`:
```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RagAgents.Api.Middleware
{
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
            _logger.LogError(exception, 
                "Unhandled exception: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = httpContext.Request.Path,
                Instance = httpContext.TraceIdentifier
            };

            // Don't expose internal errors in production
            if (!httpContext.RequestServices
                .GetRequiredService<IHostEnvironment>()
                .IsDevelopment())
            {
                problemDetails.Detail = "An error occurred processing your request";
            }
            else
            {
                problemDetails.Detail = exception.Message;
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
```

Update Program.cs:
```csharp
// Add this BEFORE builder.Build()
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add this AFTER app.UseHttpsRedirection()
app.UseExceptionHandler();
```

---

### 5. Change Service Lifetimes (2 minutes)

Update Program.cs:
```csharp
// Replace Singleton with Scoped (or use HttpClient factory)
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IAzureSearchService, AzureSearchService>();
```

---

### 6. Add Basic Health Checks (5 minutes)

Install package:
```powershell
dotnet add RagAgents.Api package Microsoft.Extensions.Diagnostics.HealthChecks
```

Update Program.cs:
```csharp
// Add before builder.Build()
builder.Services.AddHealthChecks();

// Add before app.Run()
app.MapHealthChecks("/health");
```

---

## 🎯 Verification Steps

After applying fixes:

1. **Verify secrets removed:**
   ```powershell
   # This should show empty keys
   cat appsettings.json | Select-String "Key"
   ```

2. **Test health endpoint:**
   ```powershell
   # Start app
   dotnet run --project RagAgents.Api
   
   # Test health
   curl http://localhost:5000/health
   ```

3. **Test CORS:**
   ```javascript
   // From browser console on allowed origin
   fetch('http://localhost:5000/api/chat/ping', {
       method: 'GET',
       headers: { 'Authorization': 'Bearer <token>' }
   })
   ```

4. **Test error handling:**
   - Trigger an error (e.g., invalid request)
   - Check that errors are logged
   - Verify clean error response (no stack trace in prod)

---

## ✅ Deployment Checklist

Before deploying to production:

- [ ] All secrets in Azure Key Vault or User Secrets
- [ ] No hardcoded keys in appsettings.json
- [ ] CORS configured with specific origins
- [ ] Global exception handler enabled
- [ ] Service lifetimes set to Scoped
- [ ] Health checks working
- [ ] All `throw ex;` replaced with `throw;`
- [ ] appsettings.Production.json configured
- [ ] Connection strings use managed identity (optional)

---

## 🔐 Azure Key Vault Setup (Production)

**Step 1:** Create Key Vault
```powershell
az keyvault create --name "ragagents-kv" --resource-group "rg-ragagents" --location "eastus"
```

**Step 2:** Add secrets
```powershell
az keyvault secret set --vault-name "ragagents-kv" --name "AzureOpenAI--Key" --value "your-key"
az keyvault secret set --vault-name "ragagents-kv" --name "AzureSearchAI--Key" --value "your-key"
```

**Step 3:** Enable managed identity for App Service
```powershell
az webapp identity assign --name "ragagents-api" --resource-group "rg-ragagents"
```

**Step 4:** Grant access
```powershell
az keyvault set-policy --name "ragagents-kv" --object-id <managed-identity-id> --secret-permissions get list
```

**Step 5:** Update Program.cs
```csharp
// Add NuGet: Azure.Extensions.AspNetCore.Configuration.Secrets
var keyVaultEndpoint = new Uri(builder.Configuration["KeyVaultEndpoint"]!);
builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
```

**Step 6:** Add to appsettings.Production.json
```json
{
  "KeyVaultEndpoint": "https://ragagents-kv.vault.azure.net/"
}
```

---

**Total Time: ~30 minutes for critical fixes**
