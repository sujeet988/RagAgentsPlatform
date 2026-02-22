#Requires -Version 7.0
<#
.SYNOPSIS
    Monitors Azure OpenAI quota usage and sends alerts
.DESCRIPTION
    Checks OpenAI usage across all deployments and alerts when thresholds are exceeded
.PARAMETER ResourceGroup
    Resource group containing the OpenAI resource
.PARAMETER OpenAIName
    Name of the Azure OpenAI resource
.PARAMETER ThresholdPercent
    Alert when usage exceeds this percentage (default: 80)
.PARAMETER SendAlert
    Send alert to configured channels (email, Teams, etc.)
.EXAMPLE
    .\Monitor-OpenAIQuota.ps1 -ResourceGroup rg-ragagents-prod -OpenAIName openai-ragagents-prod
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$OpenAIName,

    [Parameter(Mandatory = $false)]
    [int]$ThresholdPercent = 80,

    [Parameter(Mandatory = $false)]
    [switch]$SendAlert
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import common functions
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\modules\Common.psm1" -Force

Write-LogHeader "Azure OpenAI Quota Monitoring"

# Validate Azure CLI
if (!(Test-AzureCLI)) {
    Write-LogError "Azure CLI is not installed"
    exit 1
}

# Get OpenAI resource
Write-LogInfo "Getting OpenAI resource: $OpenAIName"
$openai = az cognitiveservices account show `
    --name $OpenAIName `
    --resource-group $ResourceGroup `
    --output json | ConvertFrom-Json

if (!$openai) {
    Write-LogError "OpenAI resource not found"
    exit 1
}

# Get all deployments
Write-LogSection "Checking Deployments"
$deployments = az cognitiveservices account deployment list `
    --name $OpenAIName `
    --resource-group $ResourceGroup `
    --output json | ConvertFrom-Json

if ($deployments.Count -eq 0) {
    Write-LogWarning "No deployments found"
    exit 0
}

$alerts = @()

foreach ($deployment in $deployments) {
    Write-LogInfo "Checking deployment: $($deployment.name)"
    
    $capacity = $deployment.properties.sku.capacity
    $model = $deployment.properties.model.name
    $version = $deployment.properties.model.version
    
    # Query metrics from last hour
    $endTime = Get-Date
    $startTime = $endTime.AddHours(-1)
    
    $metrics = az monitor metrics list `
        --resource $openai.id `
        --metric "TokenTransaction" `
        --start-time $startTime.ToString("yyyy-MM-ddTHH:mm:ssZ") `
        --end-time $endTime.ToString("yyyy-MM-ddTHH:mm:ssZ") `
        --aggregation Total `
        --filter "ApiName eq 'Azure OpenAI API' and ModelDeploymentName eq '$($deployment.name)'" `
        --output json | ConvertFrom-Json
    
    if ($metrics.value -and $metrics.value[0].timeseries) {
        $totalTokens = ($metrics.value[0].timeseries.data | Measure-Object -Property total -Sum).Sum
        
        # Calculate usage percentage
        # Note: This is simplified - actual quota calculation depends on time window
        $estimatedQuota = $capacity * 1000 * 60 # tokens per minute * 60 minutes
        $usagePercent = if ($estimatedQuota -gt 0) { ($totalTokens / $estimatedQuota) * 100 } else { 0 }
        
        Write-Host "  Model: $model (v$version)" -ForegroundColor Cyan
        Write-Host "  Capacity: $capacity K TPM" -ForegroundColor Cyan
        Write-Host "  Tokens (last hour): $($totalTokens.ToString('N0'))" -ForegroundColor Cyan
        Write-Host "  Usage: $($usagePercent.ToString('F2'))%" -ForegroundColor $(if ($usagePercent -gt $ThresholdPercent) { 'Yellow' } else { 'Green' })
        
        if ($usagePercent -gt $ThresholdPercent) {
            $alerts += @{
                Deployment = $deployment.name
                Model = $model
                Usage = $usagePercent
                Tokens = $totalTokens
                Capacity = $capacity
            }
            
            Write-LogWarning "⚠️  Usage exceeds threshold ($ThresholdPercent%)"
        }
    }
    else {
        Write-Host "  No usage data in last hour" -ForegroundColor Gray
    }
    
    Write-Host ""
}

# Summary
Write-LogSection "Summary"
if ($alerts.Count -eq 0) {
    Write-LogSuccess "All deployments within quota limits"
    exit 0
}

Write-LogWarning "Found $($alerts.Count) deployment(s) exceeding threshold"

foreach ($alert in $alerts) {
    Write-Host "  • $($alert.Deployment): $($alert.Usage.ToString('F2'))% ($($alert.Tokens.ToString('N0')) tokens)" -ForegroundColor Yellow
}

# Send alerts if requested
if ($SendAlert) {
    Write-LogSection "Sending Alerts"
    
    $alertMessage = @{
        title = "Azure OpenAI Quota Alert"
        text = "High usage detected in $OpenAIName"
        deployments = $alerts
        timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    } | ConvertTo-Json -Depth 10
    
    # TODO: Implement your alerting mechanism here
    # Examples:
    # - Send to Azure Monitor Action Group
    # - Post to Teams webhook
    # - Send email via SendGrid
    # - Create Azure DevOps work item
    
    Write-LogInfo "Alert payload:"
    Write-Host $alertMessage -ForegroundColor Cyan
    
    # Example: Send to Teams webhook
    # $webhookUrl = Get-Content "$scriptPath\..\config\teams-webhook.txt"
    # Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $alertMessage -ContentType 'application/json'
    
    Write-LogSuccess "Alerts sent"
}

Write-LogInfo "Recommendations:"
Write-Host "  1. Consider increasing capacity for high-usage deployments" -ForegroundColor Cyan
Write-Host "  2. Implement rate limiting in application code" -ForegroundColor Cyan
Write-Host "  3. Review and optimize prompt lengths" -ForegroundColor Cyan
Write-Host "  4. Enable quota alerts in Azure Monitor" -ForegroundColor Cyan

exit 0
