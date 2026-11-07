#Requires -Version 7.0
<#
.SYNOPSIS
    Scaffolds a new, empty blog sample following the repository's conventions.

.DESCRIPTION
    Creates a new sample folder containing:
      - .gitignore           (latest VisualStudio.gitignore fetched from github/gitignore)
      - README.md            (section skeleton matching the other samples)
      - doc-images/          (with a .gitkeep so git tracks the empty folder)
      - <Name>.slnx          (empty Visual Studio solution, when -VSSolution is set)
      - azure-resources/     (empty Terraform project, when -Terraform is set)

    The Terraform project pins the latest stable azurerm and azapi provider versions,
    resolved online from the Terraform Registry when the script runs.

    All network calls happen before anything is written to disk, and a partially
    generated sample is removed if any step fails.

.PARAMETER Name
    Name of the sample. Used as the folder name, the solution name and to derive
    the Azure resource group name (rg-test-<kebab-case-name>).

.PARAMETER Terraform
    Generate an empty Terraform project under azure-resources/.

.PARAMETER VSSolution
    Generate an empty Visual Studio solution (<Name>.slnx).

.PARAMETER Destination
    Folder in which the sample folder is created. Defaults to the repository root.

.EXAMPLE
    ./Tools/New-Sample.ps1 -Name AzureServiceBusSessions -Terraform -VSSolution

.EXAMPLE
    ./Tools/New-Sample.ps1 AzureStorageQueuesBasics -Terraform
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9._-]*$')]
    [Alias('SampleName')]
    [string]$Name,

    [switch]$Terraform,

    [switch]$VSSolution,

    [string]$Destination = (Split-Path -Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'

#region Helpers

function Invoke-OnlineFetch {
    param(
        [Parameter(Mandatory)][string]$Uri,
        [Parameter(Mandatory)][string]$Description
    )

    try {
        return Invoke-RestMethod -Uri $Uri -TimeoutSec 30
    }
    catch {
        throw "Failed to fetch $Description from '$Uri': $($_.Exception.Message)"
    }
}

function Get-GitHubGitignore {
    param([Parameter(Mandatory)][string]$Template)

    $uri = "https://raw.githubusercontent.com/github/gitignore/main/$Template.gitignore"
    $content = Invoke-OnlineFetch -Uri $uri -Description "$Template.gitignore template"

    if ([string]::IsNullOrWhiteSpace($content)) {
        throw "Fetched $Template.gitignore template from '$uri' is empty."
    }

    return $content.TrimEnd()
}

function Get-LatestProviderVersion {
    param(
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][string]$Type
    )

    $baseUri = "https://registry.terraform.io/v1/providers/$Namespace/$Type"
    $provider = Invoke-OnlineFetch -Uri $baseUri -Description "latest $Namespace/$Type provider version"

    if ($provider.version -match '^\d+\.\d+\.\d+$') {
        return $provider.version
    }

    # Latest published version is a pre-release: fall back to the highest stable version.
    $versions = Invoke-OnlineFetch -Uri "$baseUri/versions" -Description "$Namespace/$Type provider versions"
    $stable = $versions.versions.version |
        Where-Object { $_ -match '^\d+\.\d+\.\d+$' } |
        Sort-Object -Property { [version]$_ } |
        Select-Object -Last 1

    if (-not $stable) {
        throw "No stable version found for the $Namespace/$Type Terraform provider."
    }

    return $stable
}

function Read-EnvFile {
    param([Parameter(Mandatory)][string]$Path)

    $values = @{}
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
            $values[$Matches[1]] = $Matches[2].Trim('"', "'")
        }
    }
    return $values
}

function ConvertTo-KebabCase {
    param([Parameter(Mandatory)][string]$Value)

    $kebab = $Value -creplace '(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])', '-'
    $kebab = $kebab -replace '[._]+', '-' -replace '-{2,}', '-'
    return $kebab.Trim('-').ToLowerInvariant()
}

function Expand-Template {
    param(
        [Parameter(Mandatory)][string]$Template,
        [hashtable]$Tokens = @{}
    )

    $result = $Template
    foreach ($token in $Tokens.GetEnumerator()) {
        $result = $result.Replace("{{$($token.Key)}}", [string]$token.Value)
    }
    return $result
}

function Write-SampleFile {
    param(
        [Parameter(Mandatory)][string]$RelativePath,
        [AllowEmptyString()][string]$Content = ''
    )

    $fullPath = Join-Path -Path $script:samplePath -ChildPath $RelativePath
    $parent = Split-Path -Path $fullPath -Parent
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    # Normalize to platform-native line endings and terminate non-empty files with a newline.
    $normalized = ($Content -replace "`r`n", "`n").TrimEnd("`n")
    if ($normalized.Length -gt 0) {
        $normalized = ($normalized -replace "`n", [Environment]::NewLine) + [Environment]::NewLine
    }

    [System.IO.File]::WriteAllText($fullPath, $normalized, [System.Text.UTF8Encoding]::new($false))
    $script:createdFiles.Add($RelativePath.Replace('\', '/'))
}

#endregion

#region Templates

$azureServiceDependenciesIgnore = @'
# Ignore Azure Service dependencies files
Properties/ServiceDependencies/*
Properties/PublishProfiles/*
Properties/serviceDependencies*.json
'@

$readmeHeaderTemplate = @'
# {{NAME}}

<!-- TODO: Short description of the sample. -->

This example is discussed in the [<BLOG POST TITLE>](http://blog.techdominator.com/article/<BLOG-POST-SLUG>.html) blog post.

## Pre-Requisites
'@

$readmeVisualStudioPrerequisite = @'
- [Visual Studio 2026](https://visualstudio.microsoft.com/downloads/) or Alternatively [VS Code](https://code.visualstudio.com/) with the [C# Dev Kit Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
'@

$readmeCommonPrerequisites = @'
- [Powershell 7](https://learn.microsoft.com/en-us/powershell/scripting/install/install-powershell?view=powershell-7.5)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
- [Azure Subscription](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)
'@

$readmeTerraformPrerequisite = @'
- [Terraform](https://developer.hashicorp.com/terraform/tutorials/azure-get-started/install-cli)
'@

$readmeSetupOverview = @'

## Setup Overview

## How to use
'@

$readmeTerraformHowTo = @'
### 1. Create Azure Resources
Azure Resources for this sample can be created via the terraform project under `azure-resources`:
1. `cd azure-resources`
2. `terraform init`
3. `terraform apply -var subscription="<AZURE SUBSCRIPTION ID>" -var user_principal_id="<USER PRINCIPAL ID>"`

Terraform will ask for confirmation before applying the infrastructure resources.
'@

$readmeNotes = @'

## Notes
'@

$readmeTerraformCleanup = @'

### Azure Resources Cleanup
After finishing using the sample, remember **to remove the azure resources** to avoid incurring unnecessary costs on your Azure Subscription.

This can be done with Terraform by running:
```bash
terraform destroy -var subscription="<AZURE SUBSCRIPTION ID>" -var user_principal_id="<USER PRINCIPAL ID>"
```
'@

$readmeContributing = @'

## Contributing

Please checkout [the contribution guidelines](../CONTRIBUTING.md) for contributing.
'@

$slnxTemplate = @'
<Solution>
</Solution>
'@

$terraformTemplate = @'
terraform {
  backend "local" {}

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> {{AZURERM_VERSION}}"
    }

    azapi = {
      source  = "azure/azapi"
      version = "~> {{AZAPI_VERSION}}"
    }
  }
}
'@

$providersTemplate = @'
provider "azurerm" {
  subscription_id = var.subscription
  features {}
}

provider "azapi" {}
'@

$variablesTemplate = @'
variable "subscription" {
  type        = string
  description = "Azure subscription UUID"
}

variable "user_principal_id" {
  type        = string
  description = "User principal UUID having access on the resources"
}

variable "location" {
  type    = string
  default = "swedencentral"
}
'@

$resourceGroupTemplate = @'
data "azurerm_subscription" "main_subscription" {}

resource "azurerm_resource_group" "main_resource_group" {
  name     = "{{RESOURCE_GROUP_NAME}}"
  location = var.location
}
'@

$rollTemplate = @'
param (
    [Switch]$Destroy
)

terraform init
terraform fmt

if (-not $Destroy) {
    terraform apply -auto-approve `
        -var subscription="{{SUBSCRIPTION_ID}}" `
        -var user_principal_id="{{USER_PRINCIPAL_ID}}"
}
else {
    terraform destroy -auto-approve `
        -var subscription="{{SUBSCRIPTION_ID}}" `
        -var user_principal_id="{{USER_PRINCIPAL_ID}}"
}
'@

#endregion

#region Pre-checks

if (-not (Test-Path -LiteralPath $Destination -PathType Container)) {
    throw "Destination folder '$Destination' does not exist."
}

$Destination = (Resolve-Path -LiteralPath $Destination).ProviderPath
$script:samplePath = Join-Path -Path $Destination -ChildPath $Name
$script:createdFiles = [System.Collections.Generic.List[string]]::new()

if (Test-Path -LiteralPath $script:samplePath) {
    throw "Sample folder '$($script:samplePath)' already exists. Choose another name or remove the existing folder."
}

#endregion

#region Fetch phase (online, before anything is written)

Write-Host 'Fetching VisualStudio.gitignore template from github/gitignore...'
$rootGitignore = (Get-GitHubGitignore -Template 'VisualStudio') + "`n`n" + $azureServiceDependenciesIgnore

if ($Terraform) {
    Write-Host 'Resolving latest Terraform provider versions from registry.terraform.io...'
    $azurermVersion = Get-LatestProviderVersion -Namespace 'hashicorp' -Type 'azurerm'
    $azapiVersion = Get-LatestProviderVersion -Namespace 'azure' -Type 'azapi'

    Write-Host 'Fetching Terraform.gitignore template from github/gitignore...'
    $terraformGitignore = (Get-GitHubGitignore -Template 'Terraform') + "`n`nroll.ps1`n"

    # roll.ps1 is git-ignored, so real IDs from the local .env-sample can be hardcoded into it.
    $envFilePath = Join-Path -Path $PSScriptRoot -ChildPath '.env-sample'
    $envValues = Read-EnvFile -Path $envFilePath
    $subscriptionId = if ($envValues['AZURE_SUBSCRIPTION_ID']) { $envValues['AZURE_SUBSCRIPTION_ID'] } else { '<AZURE SUBSCRIPTION ID>' }
    $userPrincipalId = if ($envValues['USER_PRINCIPAL_ID']) { $envValues['USER_PRINCIPAL_ID'] } else { '<USER PRINCIPAL ID>' }
    $rollUsesEnvValues = $envValues['AZURE_SUBSCRIPTION_ID'] -and $envValues['USER_PRINCIPAL_ID']
}

#endregion

#region Write phase

$readmeParts = [System.Collections.Generic.List[string]]::new()
$readmeParts.Add((Expand-Template -Template $readmeHeaderTemplate -Tokens @{ NAME = $Name }))
if ($VSSolution) { $readmeParts.Add($readmeVisualStudioPrerequisite) }
$readmeParts.Add($readmeCommonPrerequisites)
if ($Terraform) { $readmeParts.Add($readmeTerraformPrerequisite) }
$readmeParts.Add($readmeSetupOverview)
if ($Terraform) { $readmeParts.Add($readmeTerraformHowTo) }
$readmeParts.Add($readmeNotes)
if ($Terraform) { $readmeParts.Add($readmeTerraformCleanup) }
$readmeParts.Add($readmeContributing)
$readme = $readmeParts -join "`n"

try {
    New-Item -ItemType Directory -Path $script:samplePath | Out-Null

    Write-SampleFile -RelativePath '.gitignore' -Content $rootGitignore
    Write-SampleFile -RelativePath 'README.md' -Content $readme
    Write-SampleFile -RelativePath 'doc-images/.gitkeep'

    if ($VSSolution) {
        Write-SampleFile -RelativePath "$Name.slnx" -Content $slnxTemplate
    }

    if ($Terraform) {
        $resourceGroupName = "rg-test-$(ConvertTo-KebabCase -Value $Name)"

        Write-SampleFile -RelativePath 'azure-resources/.gitignore' -Content $terraformGitignore
        Write-SampleFile -RelativePath 'azure-resources/_terraform.tf' -Content (Expand-Template -Template $terraformTemplate -Tokens @{
                AZURERM_VERSION = $azurermVersion
                AZAPI_VERSION   = $azapiVersion
            })
        Write-SampleFile -RelativePath 'azure-resources/_providers.tf' -Content $providersTemplate
        Write-SampleFile -RelativePath 'azure-resources/_variables.tf' -Content $variablesTemplate
        Write-SampleFile -RelativePath 'azure-resources/rg_main.tf' -Content (Expand-Template -Template $resourceGroupTemplate -Tokens @{
                RESOURCE_GROUP_NAME = $resourceGroupName
            })
        Write-SampleFile -RelativePath 'azure-resources/roll.ps1' -Content (Expand-Template -Template $rollTemplate -Tokens @{
                SUBSCRIPTION_ID   = $subscriptionId
                USER_PRINCIPAL_ID = $userPrincipalId
            })
    }
}
catch {
    if (Test-Path -LiteralPath $script:samplePath) {
        Remove-Item -LiteralPath $script:samplePath -Recurse -Force
    }
    throw
}

#endregion

#region Summary

Write-Host ''
Write-Host "Sample '$Name' created at $($script:samplePath)" -ForegroundColor Green
foreach ($file in $script:createdFiles) {
    Write-Host "  $file"
}

if ($Terraform) {
    Write-Host ''
    Write-Host 'Terraform providers:'
    Write-Host "  hashicorp/azurerm ~> $azurermVersion"
    Write-Host "  azure/azapi       ~> $azapiVersion"
    Write-Host ''
    Write-Host 'Next steps:'
    Write-Host "  cd '$(Join-Path -Path $script:samplePath -ChildPath 'azure-resources')'"
    if ($rollUsesEnvValues) {
        Write-Host '  ./roll.ps1   # IDs taken from Tools/.env-sample (roll.ps1 is git-ignored)'
    }
    else {
        Write-Host '  # Fill in the placeholder IDs in roll.ps1 (git-ignored; or add them to Tools/.env-sample), then:'
        Write-Host '  ./roll.ps1'
    }
}

#endregion
