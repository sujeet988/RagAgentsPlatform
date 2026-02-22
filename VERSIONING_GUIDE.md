# Model Versioning and Prompt Versioning Guide

## Overview

The RagAgents system now supports comprehensive **Model Versioning** and **Prompt Versioning** to enable:

✅ **A/B Testing** - Compare performance of different models and prompts  
✅ **Rollback Capability** - Revert to previous versions if issues arise  
✅ **Audit Trail** - Track which versions were used for each request  
✅ **Performance Metrics** - Monitor token usage, response times by version  
✅ **Gradual Rollouts** - Test new versions with subset of users

---

## Model Versioning

### Configuration

Model versions are configured in `appsettings.json` or `appsettings.Versioning.json`:

```json
{
  "AzureOpenAI": {
    "ChatModelVersion": "gpt-4-0613",
    "EmbeddingModelVersion": "text-embedding-3-large-1",
    "Versioning": {
      "EnableVersionTracking": true,
      "LogModelMetrics": true,
      "AvailableVersions": {
        "gpt-4-latest": {
          "Name": "GPT-4 Latest",
          "DeploymentName": "gpt-4",
          "Version": "gpt-4-0613",
          "MaxTokens": 8192,
          "IsActive": true,
          "ActivatedDate": "2024-01-15T00:00:00Z"
        }
      }
    }
  }
}
```

### Tracking Model Usage

Every OpenAI API call automatically tracks:
- Model version used
- Token consumption (prompt + completion)
- Response time
- Success/failure status

Data flows to **Application Insights** for analysis.

### API Endpoints

#### Get Current Model Versions
```http
GET /api/VersionManagement/models/current
Authorization: Bearer {token}
```

Response:
```json
{
  "chatModel": "gpt-4-0613",
  "embeddingModel": "text-embedding-3-large-1"
}
```

#### Get Model Usage Statistics
```http
GET /api/VersionManagement/models/{modelVersion}/stats?startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer {token}
```

Response:
```json
{
  "modelVersion": "gpt-4-0613",
  "totalRequests": 15420,
  "successfulRequests": 15380,
  "failedRequests": 40,
  "totalTokensUsed": 2340560,
  "averageResponseTimeMs": 1234.56,
  "successRate": 0.997
}
```

#### Switch Model Version
```http
POST /api/VersionManagement/models/switch
Authorization: Bearer {token}
Content-Type: application/json

{
  "deploymentName": "gpt-4",
  "newVersion": "gpt-4-1106-preview"
}
```

---

## Prompt Versioning

### Configuration

Prompt versions are defined in `appsettings.Versioning.json`:

```json
{
  "PromptOptions": {
    "CurrentVersion": "v2.0",
    "EnableVersionTracking": true,
    "Versions": {
      "v1.0": {
        "Version": "v1.0",
        "Description": "Initial production prompts",
        "IsActive": false,
        "SystemPrompt": "You are a helpful assistant.",
        "RagPrompts": { ... }
      },
      "v2.0": {
        "Version": "v2.0",
        "Description": "Enhanced prompts with better context handling",
        "IsActive": true,
        "SystemPrompt": "You are an enterprise AI assistant...",
        "RagPrompts": { ... }
      }
    }
  }
}
```

### Using Versioned Prompts

```csharp
// Get current version prompts (default)
var systemPrompt = _promptProvider.GetSystemPrompt();

// Get specific version
var v1Prompt = _promptProvider.GetSystemPrompt("v1.0");

// Check current version
var currentVersion = _promptProvider.GetCurrentVersion(); // "v2.0"

// Get full version details
var v2Details = _promptProvider.GetVersionedPrompts("v2.0");
```

### Tracking Prompt Usage

```csharp
await _versionTrackingService.TrackPromptUsageAsync(new PromptUsageRecord
{
    PromptVersion = "v2.0",
    PromptType = "RagWithCitations",
    Question = "What is the refund policy?",
    ModelVersion = "gpt-4-0613",
    ResponseLength = 245,
    ResponseTimeMs = 1234.56,
    IsSuccess = true,
    Timestamp = DateTime.UtcNow,
    UserId = user.Id
});
```

### API Endpoints

#### Get Current Prompt Version
```http
GET /api/VersionManagement/prompts/current
Authorization: Bearer {token}
```

Response:
```json
{
  "version": "v2.0"
}
```

#### Get Prompt Version Details
```http
GET /api/VersionManagement/prompts/v2.0
Authorization: Bearer {token}
```

Response:
```json
{
  "version": "v2.0",
  "description": "Enhanced prompts with better context handling",
  "createdDate": "2024-02-01T00:00:00Z",
  "createdBy": "admin",
  "isActive": true,
  "systemPrompt": "You are an enterprise AI assistant...",
  "ragPrompts": {
    "simpleRag": "...",
    "ragWithHistory": "...",
    "ragWithCitations": "..."
  }
}
```

#### Get Prompt Usage Statistics
```http
GET /api/VersionManagement/prompts/v2.0/stats?startDate=2024-01-01&endDate=2024-01-31
Authorization: Bearer {token}
```

#### Switch Prompt Version
```http
POST /api/VersionManagement/prompts/switch
Authorization: Bearer {token}
Content-Type: application/json

{
  "newVersion": "v2.1-experimental"
}
```

---

## A/B Testing

### Example: Test Two Prompt Versions

1. **Define versions** in configuration:
   - `v2.0` (baseline)
   - `v2.1-experimental` (test variant)

2. **Randomly assign users** to versions:
```csharp
var version = (userId % 2 == 0) ? "v2.0" : "v2.1-experimental";
var systemPrompt = _promptProvider.GetSystemPrompt(version);
```

3. **Track usage** for both versions:
```csharp
await _versionTrackingService.TrackPromptUsageAsync(new PromptUsageRecord
{
    PromptVersion = version,
    // ... other fields
});
```

4. **Compare metrics** in Application Insights:
   - Response time
   - Response quality (custom metrics)
   - User satisfaction scores

---

## Monitoring in Azure

### Application Insights Queries

**Model usage by version:**
```kusto
customEvents
| where name == "ModelUsage"
| summarize 
    Requests = count(),
    AvgTokens = avg(toint(customMeasurements["TokensUsed"])),
    AvgResponseTime = avg(toint(customMeasurements["ResponseTimeMs"]))
  by tostring(customDimensions["ModelVersion"])
```

**Prompt performance comparison:**
```kusto
customEvents
| where name == "PromptUsage"
| summarize 
    Count = count(),
    AvgResponseTime = avg(toint(customMeasurements["ResponseTimeMs"]))
  by tostring(customDimensions["PromptVersion"]), tostring(customDimensions["PromptType"])
```

**Token cost by model version:**
```kusto
customMetrics
| where name startswith "Model."
| where name endswith ".TokensUsed"
| summarize TotalTokens = sum(value) by name
```

---

## Best Practices

### Model Versioning
1. **Always track versions** in production (`EnableVersionTracking: true`)
2. **Test new models** in dev environment first
3. **Monitor quota** when switching versions (different models have different TPM limits)
4. **Document changes** in `ActivatedDate` and version descriptions

### Prompt Versioning
1. **Use semantic versioning** (v1.0, v1.1, v2.0)
2. **Add descriptions** explaining what changed
3. **Keep old versions** for rollback capability
4. **Test thoroughly** before marking `IsActive: true`
5. **Track metrics** to measure improvement

### General
- Set up **Application Insights alerts** for version-specific metrics
- Review **cost per version** regularly
- Use **gradual rollouts** (10% → 50% → 100% traffic)
- Document **rollback procedures**

---

## Integration with CI/CD

Add version info to deployment pipeline:

```yaml
- name: Deploy with Version Tracking
  run: |
    # Update app settings with new model version
    az webapp config appsettings set \
      --name ragagents-api-prod \
      --settings AzureOpenAI__ChatModelVersion="gpt-4-1106-preview"
    
    # Track deployment event
    az monitor app-insights events show \
      --app ragagents-insights-prod \
      --event "ModelVersionDeployment"
```

---

## Example Scenarios

### Scenario 1: Migrate to GPT-4 Turbo
1. Add `gpt-4-turbo` deployment in Azure OpenAI
2. Update `appsettings.json`:
   ```json
   "AvailableVersions": {
     "gpt-4-turbo": {
       "DeploymentName": "gpt-4-turbo",
       "Version": "gpt-4-1106-preview",
       "IsActive": true
     }
   }
   ```
3. Call `/api/VersionManagement/models/switch`
4. Monitor metrics in Application Insights
5. If successful, update `ChatModelVersion` in config

### Scenario 2: A/B Test Concise Prompts
1. Create `v2.1-experimental` with shorter prompts
2. Route 10% of users to new version
3. Track response times and quality metrics
4. After 1 week, compare:
   - Average response time
   - User satisfaction ratings
   - Token costs
5. If better, promote to `CurrentVersion`

---

## Troubleshooting

**Q: Version tracking not appearing in Application Insights**  
A: Check `EnableVersionTracking: true` and ensure `TelemetryClient` is registered

**Q: How to rollback a prompt version?**  
A: Call `/api/VersionManagement/prompts/switch` with previous version, restart app

**Q: Can I track custom metrics?**  
A: Yes, extend `ModelUsageRecord` and `PromptUsageRecord` with additional fields

**Q: How to export version history?**  
A: Query Application Insights API or export to Log Analytics workspace

---

## Next Steps

1. ✅ Enable version tracking in production
2. ✅ Set up Application Insights dashboards
3. ✅ Configure alerts for version-specific issues
4. ✅ Document baseline metrics for current versions
5. ✅ Plan A/B tests for prompt improvements
