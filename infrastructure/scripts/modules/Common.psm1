# Common PowerShell functions for infrastructure scripts

function Write-LogHeader {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host " $Message" -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta
}

function Write-LogSection {
    param([string]$Message)
    Write-Host "`n--- $Message ---" -ForegroundColor Blue
}

function Write-LogInfo {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor White
}

function Write-LogSuccess {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-LogWarning {
    param([string]$Message)
    Write-Host "[⚠] $Message" -ForegroundColor Yellow
}

function Write-LogError {
    param([string]$Message)
    Write-Host "[✗] $Message" -ForegroundColor Red
}

function Test-AzureCLI {
    try {
        $null = az --version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

function Test-ResourceExists {
    param(
        [string]$ResourceGroup,
        [string]$Name,
        [string]$Type
    )
    
    try {
        $result = az resource show `
            --resource-group $ResourceGroup `
            --name $Name `
            --resource-type $Type `
            2>$null
        return $null -ne $result
    }
    catch {
        return $false
    }
}

function Get-DeploymentStatus {
    param(
        [string]$DeploymentName,
        [string]$ResourceGroup = $null
    )
    
    if ($ResourceGroup) {
        az deployment group show `
            --name $DeploymentName `
            --resource-group $ResourceGroup `
            --query 'properties.provisioningState' `
            --output tsv
    }
    else {
        az deployment sub show `
            --name $DeploymentName `
            --query 'properties.provisioningState' `
            --output tsv
    }
}

Export-ModuleMember -Function *
