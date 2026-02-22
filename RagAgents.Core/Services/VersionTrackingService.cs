using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagAgents.Core.Interfaces;
using RagAgents.Core.Models;

namespace RagAgents.Core.Services
{
    /// <summary>
    /// Service for tracking model and prompt versions with telemetry
    /// </summary>
    public class VersionTrackingService : IVersionTrackingService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<VersionTrackingService> _logger;
        private readonly AzureOpenAIOptions _openAIOptions;
        private readonly PromptOptions _promptOptions;

        public VersionTrackingService(
            TelemetryClient telemetryClient,
            ILogger<VersionTrackingService> logger,
            IOptions<AzureOpenAIOptions> openAIOptions,
            IOptions<PromptOptions> promptOptions)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
            _openAIOptions = openAIOptions.Value;
            _promptOptions = promptOptions.Value;
        }

        public Task TrackModelUsageAsync(ModelUsageRecord record)
        {
            if (!_openAIOptions.Versioning.EnableVersionTracking)
                return Task.CompletedTask;

            // Track as custom event in Application Insights
            var eventTelemetry = new EventTelemetry("ModelUsage");
            eventTelemetry.Properties["ModelVersion"] = record.ModelVersion;
            eventTelemetry.Properties["DeploymentName"] = record.DeploymentName;
            eventTelemetry.Properties["OperationType"] = record.OperationType;
            eventTelemetry.Properties["UserId"] = record.UserId;
            eventTelemetry.Properties["IsSuccess"] = record.IsSuccess.ToString();
            
            eventTelemetry.Metrics["TokensUsed"] = record.TokensUsed;
            eventTelemetry.Metrics["PromptTokens"] = record.PromptTokens;
            eventTelemetry.Metrics["CompletionTokens"] = record.CompletionTokens;
            eventTelemetry.Metrics["ResponseTimeMs"] = record.ResponseTimeMs;

            if (!string.IsNullOrEmpty(record.ErrorMessage))
            {
                eventTelemetry.Properties["ErrorMessage"] = record.ErrorMessage;
            }

            _telemetryClient.TrackEvent(eventTelemetry);

            // Also track as custom metric for easier aggregation
            _telemetryClient.GetMetric($"Model.{record.ModelVersion}.TokensUsed").TrackValue(record.TokensUsed);
            _telemetryClient.GetMetric($"Model.{record.ModelVersion}.ResponseTime").TrackValue(record.ResponseTimeMs);

            _logger.LogInformation(
                "Model usage tracked: {ModelVersion} | {OperationType} | {TokensUsed} tokens | {ResponseTimeMs}ms",
                record.ModelVersion, record.OperationType, record.TokensUsed, record.ResponseTimeMs);

            return Task.CompletedTask;
        }

        public Task TrackPromptUsageAsync(PromptUsageRecord record)
        {
            if (!_promptOptions.EnableVersionTracking)
                return Task.CompletedTask;

            var eventTelemetry = new EventTelemetry("PromptUsage");
            eventTelemetry.Properties["PromptVersion"] = record.PromptVersion;
            eventTelemetry.Properties["PromptType"] = record.PromptType;
            eventTelemetry.Properties["ModelVersion"] = record.ModelVersion;
            eventTelemetry.Properties["UserId"] = record.UserId;
            eventTelemetry.Properties["IsSuccess"] = record.IsSuccess.ToString();
            
            eventTelemetry.Metrics["ResponseLength"] = record.ResponseLength;
            eventTelemetry.Metrics["ResponseTimeMs"] = record.ResponseTimeMs;

            _telemetryClient.TrackEvent(eventTelemetry);

            _telemetryClient.GetMetric($"Prompt.{record.PromptVersion}.Usage").TrackValue(1);
            _telemetryClient.GetMetric($"Prompt.{record.PromptVersion}.ResponseTime").TrackValue(record.ResponseTimeMs);

            _logger.LogInformation(
                "Prompt usage tracked: {PromptVersion} | {PromptType} | {ModelVersion}",
                record.PromptVersion, record.PromptType, record.ModelVersion);

            return Task.CompletedTask;
        }

        public Task<ModelUsageStats> GetModelUsageStatsAsync(string modelVersion, DateTime startDate, DateTime endDate)
        {
            // In a real implementation, this would query Application Insights Analytics API
            // or a dedicated database where version tracking data is stored
            
            _logger.LogInformation(
                "Retrieving model usage stats for {ModelVersion} from {StartDate} to {EndDate}",
                modelVersion, startDate, endDate);

            // Placeholder implementation
            var stats = new ModelUsageStats
            {
                ModelVersion = modelVersion,
                TotalRequests = 0,
                SuccessfulRequests = 0,
                FailedRequests = 0,
                TotalTokensUsed = 0,
                AverageResponseTimeMs = 0
            };

            return Task.FromResult(stats);
        }

        public Task<PromptUsageStats> GetPromptUsageStatsAsync(string promptVersion, DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation(
                "Retrieving prompt usage stats for {PromptVersion} from {StartDate} to {EndDate}",
                promptVersion, startDate, endDate);

            var stats = new PromptUsageStats
            {
                PromptVersion = promptVersion,
                TotalUsages = 0,
                SuccessfulUsages = 0,
                AverageResponseTimeMs = 0,
                AverageResponseLength = 0
            };

            return Task.FromResult(stats);
        }

        public Task<bool> SwitchModelVersionAsync(string deploymentName, string newVersion)
        {
            _logger.LogWarning(
                "Model version switch requested: {DeploymentName} -> {NewVersion}. " +
                "This requires updating Azure OpenAI deployment configuration.",
                deploymentName, newVersion);

            // Track the version switch event
            var eventTelemetry = new EventTelemetry("ModelVersionSwitch");
            eventTelemetry.Properties["DeploymentName"] = deploymentName;
            eventTelemetry.Properties["NewVersion"] = newVersion;
            eventTelemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            
            _telemetryClient.TrackEvent(eventTelemetry);

            // In practice, this would call Azure Management API to update deployment
            return Task.FromResult(false);
        }

        public Task<bool> SwitchPromptVersionAsync(string newVersion)
        {
            if (!_promptOptions.Versions.ContainsKey(newVersion))
            {
                _logger.LogWarning("Prompt version {Version} not found in configuration", newVersion);
                return Task.FromResult(false);
            }

            _logger.LogInformation("Switching to prompt version: {Version}", newVersion);

            var eventTelemetry = new EventTelemetry("PromptVersionSwitch");
            eventTelemetry.Properties["NewVersion"] = newVersion;
            eventTelemetry.Properties["PreviousVersion"] = _promptOptions.CurrentVersion;
            eventTelemetry.Properties["Timestamp"] = DateTime.UtcNow.ToString("O");
            
            _telemetryClient.TrackEvent(eventTelemetry);

            // In practice, this would update configuration dynamically
            // For now, requires app restart with updated config
            return Task.FromResult(true);
        }
    }
}
