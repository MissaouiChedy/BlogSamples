<#
.SYNOPSIS
    Builds, packages, and deploys the IncidentAgent solution components to Azure.

.DESCRIPTION
    Deploys in this order:
      1. IncidentAgent.Mcp              -> azurerm_windows_web_app.mcp_server
      2. IncidentAgent.ResolutionTrigger -> azurerm_windows_function_app.main_azure_function_app
      3. IncidentAgent.Web              -> azurerm_windows_web_app.app

    Resource names are read from the Terraform state in .\azure-resources so this
    script picks up the same suffix that Terraform generated.

.PARAMETER Configuration
    dotnet publish configuration. Defaults to Release.

.PARAMETER SkipBuild
    Skip dotnet publish and reuse any existing artifacts in .\artifacts.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot       = $PSScriptRoot
$tfDir          = Join-Path $repoRoot "azure-resources"
$artifactsDir   = Join-Path $repoRoot "artifacts"

# ---------------------------------------------------------------------------
# Prereqs
# ---------------------------------------------------------------------------
foreach ($cmd in @("az", "dotnet", "terraform")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        throw "Required command '$cmd' was not found in PATH."
    }
}

$account = az account show --only-show-errors 2>$null | ConvertFrom-Json
if (-not $account) {
    throw "Not logged in to Azure CLI. Run 'az login' first."
}
Write-Host "Using Azure subscription: $($account.name) ($($account.id))" -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# Read Terraform outputs / state for the deployed resource names
# ---------------------------------------------------------------------------
Push-Location $tfDir
try {
    Write-Host "Reading Terraform state for resource names..." -ForegroundColor Cyan
    $tfStateJson = terraform show -json | ConvertFrom-Json

    $resources = $tfStateJson.values.root_module.resources

    function Get-TfResource {
        param([string]$Type, [string]$Name)
        $r = $resources | Where-Object { $_.type -eq $Type -and $_.name -eq $Name } | Select-Object -First 1
        if (-not $r) { throw "Could not find $Type.$Name in Terraform state." }
        return $r.values
    }

    $mcpApp   = Get-TfResource -Type "azurerm_windows_web_app"      -Name "mcp_server"
    $funcApp  = Get-TfResource -Type "azurerm_windows_function_app" -Name "main_azure_function_app"
    $webApp   = Get-TfResource -Type "azurerm_windows_web_app"      -Name "app"

    $resourceGroup = $mcpApp.resource_group_name
    $mcpAppName    = $mcpApp.name
    $funcAppName   = $funcApp.name
    $webAppName    = $webApp.name
}
finally {
    Pop-Location
}

Write-Host "  Resource group : $resourceGroup"
Write-Host "  MCP web app    : $mcpAppName"
Write-Host "  Function app   : $funcAppName"
Write-Host "  Web app        : $webAppName"

# ---------------------------------------------------------------------------
# Build / package
# ---------------------------------------------------------------------------
$projects = @(
    [pscustomobject]@{
        Name    = "IncidentAgent.Mcp"
        Project = Join-Path $repoRoot "IncidentAgent.Mcp\IncidentAgent.Mcp.csproj"
        Out     = Join-Path $artifactsDir "IncidentAgent.Mcp"
        Zip     = Join-Path $artifactsDir "IncidentAgent.Mcp.zip"
        Target  = $mcpAppName
        Kind    = "webapp"
    },
    [pscustomobject]@{
        Name    = "IncidentAgent.ResolutionTrigger"
        Project = Join-Path $repoRoot "IncidentAgent.ResolutionTrigger\IncidentAgent.ResolutionTrigger.csproj"
        Out     = Join-Path $artifactsDir "IncidentAgent.ResolutionTrigger"
        Zip     = Join-Path $artifactsDir "IncidentAgent.ResolutionTrigger.zip"
        Target  = $funcAppName
        Kind    = "functionapp"
    },
    [pscustomobject]@{
        Name    = "IncidentAgent.Web"
        Project = Join-Path $repoRoot "IncidentAgent.Web\IncidentAgent.Web\IncidentAgent.Web.csproj"
        Out     = Join-Path $artifactsDir "IncidentAgent.Web"
        Zip     = Join-Path $artifactsDir "IncidentAgent.Web.zip"
        Target  = $webAppName
        Kind    = "webapp"
    }
)

if (-not $SkipBuild) {
    if (Test-Path $artifactsDir) { Remove-Item $artifactsDir -Recurse -Force }
    New-Item -ItemType Directory -Path $artifactsDir | Out-Null

    foreach ($p in $projects) {
        Write-Host ""
        Write-Host "Publishing $($p.Name)..." -ForegroundColor Cyan
        dotnet publish $p.Project -c $Configuration -o $p.Out --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $($p.Name)." }

        Write-Host "Packaging $($p.Name) -> $($p.Zip)" -ForegroundColor Cyan
        if (Test-Path $p.Zip) { Remove-Item $p.Zip -Force }
        Compress-Archive -Path (Join-Path $p.Out "*") -DestinationPath $p.Zip -CompressionLevel Fastest -Force
    }
}
else {
    foreach ($p in $projects) {
        if (-not (Test-Path $p.Zip)) {
            throw "-SkipBuild was specified but $($p.Zip) does not exist."
        }
    }
}

# ---------------------------------------------------------------------------
# Deploy in order: MCP -> Function -> Web
# ---------------------------------------------------------------------------
foreach ($p in $projects) {
    Write-Host ""
    Write-Host "Deploying $($p.Name) to $($p.Target) ($($p.Kind))..." -ForegroundColor Green

    switch ($p.Kind) {
        "webapp" {
            az webapp deploy `
                --resource-group $resourceGroup `
                --name $p.Target `
                --src-path $p.Zip `
                --type zip `
                --only-show-errors | Out-Host
        }
        "functionapp" {
            az functionapp deployment source config-zip `
                --resource-group $resourceGroup `
                --name $p.Target `
                --src $p.Zip `
                --only-show-errors | Out-Host
        }
    }

    if ($LASTEXITCODE -ne 0) { throw "Deployment failed for $($p.Name)." }
    Write-Host "Deployed $($p.Name)." -ForegroundColor Green
}

Write-Host ""
Write-Host "All deployments completed successfully." -ForegroundColor Green
Write-Host "  MCP server : https://$($mcpApp.default_hostname)"
Write-Host "  Function   : https://$($funcApp.default_hostname)"
Write-Host "  Web app    : https://$($webApp.default_hostname)"
