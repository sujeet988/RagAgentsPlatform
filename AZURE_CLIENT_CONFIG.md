# Azure Client Configuration Guide

## Overview

The `AzureClientExtensions` provides a clean way to register Azure clients (OpenAI, Search) with environment-based authentication.

## Features

✅ **Environment Detection**
- Development: Uses API Keys from configuration
- Production: Uses Azure Managed Identity (no keys needed)

✅ **Clean Separation**
- All client registration logic in one place
- Easy to maintain and test
- Follows extension method pattern

✅ **Proper Lifetimes**
- Clients: Singleton (shared, thread-safe)
- Services: Scoped (per-request isolation)
- RAG Service: Transient (new instance per usage)

---

## Usage

### In Program.cs

```csharp
// Configure options
builder.Services.Configure<AzureOpenAIOptions>(
    builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<AzureSearchAIOptions>(
    builder.Configuration.GetSection("AzureSearchAI"));
builder.Services.Configure<PromptOptions>(
    builder.Configuration.GetSection("PromptOptions"));

// Register Azure clients (2 lines instead of 70!)
builder.Services.AddAzureClients(builder.Configuration, builder.Environment);
builder.Services.AddRagServices();
```

---

## How It Works

### Development Environment

**When**: `ASPNETCORE_ENVIRONMENT=Development`

**Authentication**: API Keys from appsettings.json or User Secrets

**Configuration**:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Key": "your-api-key",  // ← Required in Development
    "EmbeddingDeployment": "text-embedding-3-large",
    "ChatDeployment": "gpt-4"
  },
  "AzureSearchAI": {
    "Endpoint": "https://your-search.search.windows.net",
    "Key": "your-search-key",  // ← Required in Development
    "IndexName": "documents-index"
  }
}
```

**Commands**:
```powershell
# Store keys securely in User Secrets (not in git)
dotnet user-secrets set "AzureOpenAI:Key" "<your-key>"
dotnet user-secrets set "AzureSearchAI:Key" "<your-key>"
```

---

### Production Environment

**When**: `ASPNETCORE_ENVIRONMENT=Production`

**Authentication**: Azure Managed Identity (no keys needed!)

**Configuration**:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "Key": "",  // ← Empty! Uses Managed Identity
    "EmbeddingDeployment": "text-embedding-3-large",
    "ChatDeployment": "gpt-4"
  },
  "AzureSearchAI": {
    "Endpoint": "https://your-search.search.windows.net",
    "Key": "",  // ← Empty! Uses Managed Identity
    "IndexName": "documents-index"
  }
}
```

**Setup Steps**:

1. **Enable Managed Identity on App Service**:
```powershell
az webapp identity assign \
  --name <app-name> \
  --resource-group <resource-group>
```

2. **Grant Azure OpenAI Permission**:
```powershell
az role assignment create \
  --assignee <managed-identity-principal-id> \
  --role "Cognitive Services OpenAI User" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.CognitiveServices/accounts/<openai-name>
```

3. **Grant Azure Search Permission**:
```powershell
az role assignment create \
  --assignee <managed-identity-principal-id> \
  --role "Search Index Data Contributor" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.Search/searchServices/<search-name>
```

---

## Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `AzureOpenAIClient` | Singleton | Thread-safe, expensive to create |
| `SearchClient` | Singleton | Thread-safe, connection pooling |
| `SearchIndexClient` | Singleton | Admin operations, rarely used |
| `IAzureOpenAIService` | Scoped | Per-request isolation |
| `IAzureSearchService` | Scoped | Per-request isolation |
| `IPromptProvider` | Singleton | Stateless configuration reader |
| `IConversationStoreInMemory` | Singleton | Shared in-memory cache |
| `IRagService` | Transient | New instance per operation |

---

## Extension Methods

### AddAzureClients

Registers all Azure SDK clients with proper authentication.

**Parameters**:
- `IConfiguration configuration` - App configuration
- `IHostEnvironment environment` - Environment info (Dev/Prod)

**Registered Services**:
- `AzureOpenAIClient`
- `SearchClient`
- `SearchIndexClient`

### AddRagServices

Registers all RAG-related application services.

**Registered Services**:
- `IAzureOpenAIService`
- `IAzureSearchService`
- `IConversationStoreInMemory`
- `IPromptProvider`
- `IRagService`

---

## Benefits

### Before (Program.cs: 70+ lines)
```csharp
builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
    if (!string.IsNullOrEmpty(options.Key))
    {
        return new AzureOpenAIClient(...);
    }
    return new AzureOpenAIClient(...);
});
// ... repeat for each client
// ... repeat for each service
```

### After (Program.cs: 2 lines)
```csharp
builder.Services.AddAzureClients(builder.Configuration, builder.Environment);
builder.Services.AddRagServices();
```

✅ **Cleaner**: 2 lines instead of 70+  
✅ **Maintainable**: All registration logic in one file  
✅ **Testable**: Can mock `IHostEnvironment` for testing  
✅ **Reusable**: Use in multiple projects  

---

## Testing

### Unit Testing with Mock Environment

```csharp
[Test]
public void AddAzureClients_InDevelopment_UsesApiKey()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration(withKey: true);
    var environment = CreateMockEnvironment(isDevelopment: true);
    
    // Act
    services.AddAzureClients(configuration, environment);
    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<AzureOpenAIClient>();
    
    // Assert
    Assert.NotNull(client);
}

[Test]
public void AddAzureClients_InProduction_UsesManagedIdentity()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration(withKey: false);
    var environment = CreateMockEnvironment(isDevelopment: false);
    
    // Act
    services.AddAzureClients(configuration, environment);
    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<AzureOpenAIClient>();
    
    // Assert
    Assert.NotNull(client);
}
```

---

## Troubleshooting

### Issue: "Authorization failed" in Production

**Cause**: Managed Identity not configured or lacks permissions

**Fix**:
1. Check Managed Identity is enabled:
   ```powershell
   az webapp identity show --name <app-name> --resource-group <rg>
   ```

2. Verify role assignments:
   ```powershell
   az role assignment list --assignee <principal-id>
   ```

3. Check Azure portal → App Service → Identity → System assigned → Status should be "On"

### Issue: "Key cannot be empty" in Development

**Cause**: User Secrets not configured

**Fix**:
```powershell
cd RagAgents.Api
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Key" "<your-key>"
```

### Issue: Services not resolving in DI

**Cause**: Missing `AddRagServices()` call

**Fix**:
```csharp
// Must call both methods
builder.Services.AddAzureClients(builder.Configuration, builder.Environment);
builder.Services.AddRagServices();  // ← Don't forget this
```

---

## Security Best Practices

✅ **DO**:
- Use User Secrets for local development
- Use Managed Identity in production
- Leave `Key` empty in production config
- Add appsettings files to .gitignore
- Rotate keys regularly

❌ **DON'T**:
- Commit keys to source control
- Use same keys for dev and prod
- Share keys between developers
- Hardcode keys in source code
- Store keys in plain text files

---

## Migration Guide

If you're migrating from manual registration:

1. **Install packages** (if not already):
   ```powershell
   dotnet add RagAgents.Core package Azure.Identity
   dotnet add RagAgents.Core package Microsoft.Extensions.Configuration.Abstractions
   dotnet add RagAgents.Core package Microsoft.Extensions.Hosting.Abstractions
   ```

2. **Replace registration code** in Program.cs:
   ```csharp
   // Remove all manual AddSingleton<> calls
   // Replace with:
   builder.Services.AddAzureClients(builder.Configuration, builder.Environment);
   builder.Services.AddRagServices();
   ```

3. **Update using statements**:
   ```csharp
   using RagAgents.Core.Extensions;
   ```

4. **Test locally** with User Secrets

5. **Deploy to production** with Managed Identity configured

---

## Related Files

- [AzureClientExtensions.cs](../RagAgents.Core/Extensions/AzureClientExtensions.cs) - Extension method implementation
- [Program.cs](../RagAgents.Api/Program.cs) - Usage example
- [ARCHITECTURE_REVIEW.md](ARCHITECTURE_REVIEW.md) - Full architecture documentation
- [QUICK_FIXES.md](QUICK_FIXES.md) - Production setup guide
