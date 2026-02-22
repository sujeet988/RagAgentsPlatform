#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys Azure infrastructure using Bicep templates
.DESCRIPTION
    Orchestrates the deployment of RagAgents infrastructure to Azure using Bicep
.PARAMETER Environment
    Target environment (dev, staging, prod)
.PARAMETER Location
    Azure region for deployment
.PARAMETER DeployPrivateEndpoints
    Whether to deploy private endpoints (recommended for production)
.PARAMETER WhatIf
    Preview changes without deploying
.EXAMPLE
    .\Deploy-Infrastructure.ps1 -Environment dev -Location eastus
.EXAMPLE
    .\Deploy-Infrastructure.ps1 -Environment prod -Location eastus -DeployPrivateEndpoints -WhatIf
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$Location = 'eastus',

    [Parameter(Mandatory = $false)]
    [switch]$DeployPrivateEndpoints,

    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Import common functions
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module "$scriptPath\modules\Common.psm1" -Force

Write-LogHeader "RagAgents Infrastructure Deployment"

# Validate Azure CLI
Write-LogInfo "Validating prerequisites..."
if (!(Test-AzureCLI)) {
    Write-LogError "Azure CLI is not installed or not in PATH"
    exit 1
}

# Check if logged in
$account = az account show 2>$null | ConvertFrom-Json
if (!$account) {
    Write-LogWarning "Not logged into Azure. Logging in..."
    az login
    $account = az account show | ConvertFrom-Json
}

Write-LogInfo "Using subscription: $($account.name) ($($account.id))"

# Get tenant ID
$tenantId = $account.tenantId
Write-LogInfo "Tenant ID: $tenantId"

# Set deployment name
$deploymentName = "ragagents-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Build parameters
$parameters = @{
    environment = $Environment
    location = $Location
    appName = 'ragagents'
    tenantId = $tenantId
    deployPrivateEndpoints = $DeployPrivateEndpoints.IsPresent
}

# Convert to JSON for Bicep
$parametersJson = $parameters | ConvertTo-Json -Depth 10

Write-LogInfo "Deployment parameters:"
Write-Host $parametersJson -ForegroundColor Cyan

# Validate Bicep template
Write-LogSection "Validating Bicep template"
$bicepFile = "$scriptPath\..\bicep\main.bicep"

try {
    az deployment sub validate `
        --location $Location `
        --template-file $bicepFile `
        --parameters $parametersJson `
        --query 'properties.provisioningState' `
        --output tsv
    
    Write-LogSuccess "Template validation passed"
}
catch {
    Write-LogError "Template validation failed: $_"
    exit 1
}

# Deploy infrastructure
if ($WhatIf) {
    Write-LogSection "What-If Deployment (Preview Changes)"
    
    az deployment sub what-if `
        --location $Location `
        --name $deploymentName `
        --template-file $bicepFile `
        --parameters $parametersJson
    
    Write-LogInfo "What-If complete. No changes deployed."
    exit 0
}

Write-LogSection "Deploying Infrastructure"
Write-LogWarning "This will create/update Azure resources. Continue? (Y/N)"
$confirmation = Read-Host
if ($confirmation -ne 'Y') {
    Write-LogInfo "Deployment cancelled"
    exit 0
}

# Start deployment
$startTime = Get-Date
Write-LogInfo "Starting deployment: $deploymentName"

try {
    $result = az deployment sub create `
        --location $Location `
        --name $deploymentName `
        --template-file $bicepFile `
        --parameters $parametersJson `
        --output json | ConvertFrom-Json
    
    $duration = (Get-Date) - $startTime
    Write-LogSuccess "Deployment completed in $($duration.TotalMinutes.ToString('F2')) minutes"
    
    # Display outputs
    Write-LogSection "Deployment Outputs"
    $outputs = $result.properties.outputs
    
    Write-Host "Resource Group: " -NoNewline
    Write-Host $outputs.resourceGroupName.value -ForegroundColor Green
    
    Write-Host "API URL: " -NoNewline
    Write-Host $outputs.apiUrl.value -ForegroundColor Green
    
    Write-Host "Function URL: " -NoNewline
    Write-Host $outputs.functionUrl.value -ForegroundColor Green
    
    Write-Host "Key Vault: " -NoNewline
    Write-Host $outputs.keyVaultName.value -ForegroundColor Green
    
    Write-Host "OpenAI Endpoint: " -NoNewline
    Write-Host $outputs.openAIEndpoint.value -ForegroundColor Green
    
    Write-Host "Search Endpoint: " -NoNewline
    Write-Host $outputs.searchEndpoint.value -ForegroundColor Green
    
    # Save outputs to file
    $outputFile = "$scriptPath\..\outputs\$Environment-outputs.json"
    New-Item -ItemType Directory -Force -Path (Split-Path $outputFile) | Out-Null
    $outputs | ConvertTo-Json -Depth 10 | Out-File $outputFile
    Write-LogInfo "Outputs saved to: $outputFile"
    
    # Wait for RBAC propagation
    Write-LogSection "Waiting for RBAC propagation"
    Write-LogInfo "Waiting 60 seconds for role assignments to propagate..."
    Start-Sleep -Seconds 60
    
    Write-LogSuccess "Deployment complete!"
    Write-LogInfo "Next steps:"
    Write-Host "  1. Deploy application code using GitHub Actions" -ForegroundColor Cyan
    Write-Host "  2. Configure Azure AD app registration" -ForegroundColor Cyan
    Write-Host "  3. Run health checks: .\Test-Deployment.ps1 -Environment $Environment" -ForegroundColor Cyan
}
catch {
    $duration = (Get-Date) - $startTime
    Write-LogError "Deployment failed after $($duration.TotalMinutes.ToString('F2')) minutes"
    Write-LogError $_.Exception.Message
    
    # Get deployment errors
    Write-LogSection "Deployment Errors"
    az deployment sub show --name $deploymentName --query 'properties.error' --output json
    
    exit 1
}
