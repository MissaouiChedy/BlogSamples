<#
.SYNOPSIS
Builds, packages, and deploys the Async Processing App Azure Function using ZIP deployment.

.DESCRIPTION
This script performs an end-to-end deployment for the Async Processing App Azure Function:
1. Cleans the project with dotnet clean.
2. Publishes the function project with dotnet publish.
3. Creates a ZIP package from the publish output.
4. Deploys the ZIP to Azure with az functionapp deployment source config-zip.

The script stops on errors and validates native command exit codes after each step.

.PARAMETER ProjectPath
Path to the Azure Functions project file (.csproj) to build and publish.

.PARAMETER BuildConfiguration
Build configuration passed to dotnet clean and dotnet publish. Typical values are Release or Debug.

.PARAMETER RuntimeIdentifier
Runtime identifier (RID) used for publish output. Default is linux-x64.

.PARAMETER PublishOutputDirectory
Directory where dotnet publish writes compiled deployment artifacts.

.PARAMETER ZipPath
Full path of the ZIP archive created from the publish output and used for deployment.

.PARAMETER ResourceGroupName
Name of the Azure resource group that contains the target Function App.

.PARAMETER FunctionAppName
Name of the target Azure Function App to receive the ZIP deployment.

.EXAMPLE
./Invoke-DeployAzFunction.ps1

Runs deployment using all default parameter values defined in the script.

.EXAMPLE
./Invoke-DeployAzFunction.ps1 -ResourceGroupName "rg-prod" -FunctionAppName "func-prod-app"

Deploys to a specific Function App in a specific resource group.

.EXAMPLE
./Invoke-DeployAzFunction.ps1 -BuildConfiguration Debug -RuntimeIdentifier linux-x64

Builds and publishes with a non-default build configuration before deployment.

.NOTES
Prerequisites:
- dotnet SDK installed and available on PATH.
- Azure CLI installed and logged in (az login).
- Sufficient RBAC permissions to deploy to the target Function App.
#>
[CmdletBinding()]
param(
    [string]$ProjectPath = "$PSScriptRoot/../AsyncProcessingApp/AsyncProcessingApp/AsyncProcessingApp.csproj",

    [string]$BuildConfiguration = "Release",

    [string]$RuntimeIdentifier = "linux-x64",

    [string]$PublishOutputDirectory = "$PSScriptRoot/../AsyncProcessingApp/AsyncProcessingApp/bin/Release/net10.0/linux-x64/publish",

    [string]$ZipPath = "$PSScriptRoot/../AsyncProcessingApp/AsyncProcessingApp/bin/Release/net10.0/linux-x64/deploy.zip",

    [string]$ResourceGroupName = "rg-test-eventhub-load-tests-463e",

    [string]$FunctionAppName = "func-main-loadtest-463e"
)

$ErrorActionPreference = "Stop"

function Invoke-CliCommand {
    <#
    .SYNOPSIS
        Runs a script block and validates its native exit code.
    #>
    param(
        [scriptblock]$Action
    )

    $stderrPath = [System.IO.Path]::GetTempFileName()
    try {
        $output = & $Action 2> $stderrPath
        if ($LASTEXITCODE -ne 0) {
            $stderr = (Get-Content -LiteralPath $stderrPath -Raw)
            if ([string]::IsNullOrWhiteSpace($stderr)) {
                throw "Command failed with exit code $LASTEXITCODE."
            }

            throw $stderr.TrimEnd()
        }

        return $output
    }
    finally {
        if (Test-Path -LiteralPath $stderrPath) {
            Remove-Item -LiteralPath $stderrPath -Force
        }
    }
}

function Remove-IfExists {
    <#
    .SYNOPSIS
        Removes a file or directory if it exists.
    #>
    param(
        [string]$Path
    )

    if (Test-Path -Path $Path) {
        Remove-Item -Path $Path -Recurse -Force
    }
}


Write-Host "Target function app: $FunctionAppName"
Write-Host "Resource group: $ResourceGroupName"
Write-Host "Build configuration: $BuildConfiguration"
Write-Host "Runtime identifier: $RuntimeIdentifier"

Invoke-CliCommand -Action {
    dotnet clean $ProjectPath `
    --configuration $BuildConfiguration `
        --nologo
}

Remove-IfExists -Path $PublishOutputDirectory

Invoke-CliCommand -Action {
    dotnet publish $ProjectPath `
    --configuration $BuildConfiguration `
    --runtime $RuntimeIdentifier `
        --self-contained false `
        --output $PublishOutputDirectory `
        --nologo
}

Remove-IfExists -Path $ZipPath

Compress-Archive `
    -Path "$PublishOutputDirectory/*" `
    -DestinationPath $ZipPath `
    -Force

Write-Host "Deployment to '$FunctionAppName' started..."

Invoke-CliCommand -Action {
    az functionapp deployment source config-zip `
    --resource-group $ResourceGroupName `
    --name $FunctionAppName `
        --src $ZipPath `
        --build-remote false
}

Write-Host "Deployment to '$FunctionAppName' completed successfully."
