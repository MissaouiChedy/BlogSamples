<#
.SYNOPSIS
Deploys the Locust-based Azure Load Testing scenario to an existing Azure Load Testing resource.

.DESCRIPTION
Creates or updates (via remove/re-create) an Azure Load Testing test, uploads the Locust test script and Python requirements,
connects the Application Insights resource as a monitored app component, and attaches the server-side metrics
that should be collected during the run.

The script expects Azure CLI to be installed and authenticated for the target subscription.

.PARAMETER ResourceGroupName
The resource group that contains the Azure Load Testing resource, the managed identity, and the Application Insights resource.

.PARAMETER LoadTestResourceName
The name of the Azure Load Testing resource to target.

.PARAMETER SubscriptionId
The Azure subscription ID that contains the target resources.

.PARAMETER LoadTestIdentityName
The name of the user-assigned managed identity used by the load test engine.

.PARAMETER ApplicationInsightsResourceName
The Application Insights resource name that will be attached to the test for server-side metrics.

.PARAMETER EventHubNamespace
The fully qualified Event Hubs namespace used by the Locust test.

.PARAMETER EventHubName
The Event Hub name used by the Locust test.

.EXAMPLE
./Invoke-DeployLoadTests.ps1

Deploys the load test using the built-in default values.

.EXAMPLE
./Invoke-DeployLoadTests.ps1 -ResourceGroupName "rg-my-loadtests" -LoadTestResourceName "lt-my-loadtests" -SubscriptionId "00000000-0000-0000-0000-000000000000" -LoadTestIdentityName "id-my-load-test" -ApplicationInsightsResourceName "appi-my-app" -EventHubNamespace "my-namespace.servicebus.windows.net" -EventHubName "main-topic"

Deploys the load test against a custom resource group and resource naming scheme.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ResourceGroupName = "rg-test-eventhub-load-tests-463e",

    [Parameter()]
    [string]$LoadTestResourceName = "lt-main-loadtest-463e",

    [Parameter()]
    [string]$SubscriptionId = "3551d801-11f9-4f23-b222-ed3d7a9c3de8",

    [Parameter()]
    [string]$LoadTestIdentityName = "id-main-load-test-identity",

    [Parameter()]
    [string]$ApplicationInsightsResourceName = "appi-mainfuncapp-appinsights-463e",

    [Parameter()]
    [string]$EventHubNamespace = "evh-test-main-eventhub-ns-463e.servicebus.windows.net",

    [Parameter()]
    [string]$EventHubName = "main-topic"
)

$ErrorActionPreference = "Stop"

function Invoke-CliCommand {
    <#
	.SYNOPSIS
		Runs a script block and validates its native exit code.
	#>
    [CmdletBinding()]
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

function Invoke-AzCliTrimmedOutput {
    <#
	.SYNOPSIS
		Runs an Azure CLI command and returns trimmed output.
	#>
    [CmdletBinding()]
    param(
        [scriptblock]$Action
    )

    $output = Invoke-CliCommand -Action $Action
    return ($output | Out-String).Trim()
}

function Test-LoadTestExists {
    <#
	.SYNOPSIS
		Returns whether the Azure Load Testing test already exists.
	#>
    [CmdletBinding()]
    param()

    $testCountOutput = Invoke-AzCliTrimmedOutput -Action {
        az load test list `
            --load-test-resource $LoadTestResourceName `
            --resource-group $ResourceGroupName `
            --query "[?testId=='$TestId'] | length(@)" `
            --output tsv `
            --only-show-errors
    }

    [int]$testCount = 0
    if (-not [int]::TryParse($testCountOutput, [ref]$testCount)) {
        throw "Unable to parse az load test list result '$testCountOutput' as an integer test count."
    }

    return $testCount -gt 0
}

function Remove-LoadTest {
    <#
	.SYNOPSIS
		Deletes an Azure Load Testing test.
	#>
    [CmdletBinding()]
    param()

    Invoke-CliCommand -Action {
        az load test delete `
            --test-id $TestId `
            --load-test-resource $LoadTestResourceName `
            --resource-group $ResourceGroupName `
            --yes `
            --only-show-errors
    }
}
function Set-LoadTest {
    <#
	.SYNOPSIS
		Creates a Locust Azure Load Testing test through Azure CLI.
	#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$EngineIdentityResourceId,

        [Parameter(Mandatory)]
        [string]$MetricsReferenceIdentityResourceId
    )

    $envArgs = @(
        "EVENTHUB_NAME=$EventHubName",
        "EVENTHUB_FULLY_QUALIFIED_NAMESPACE=$EventHubNamespace",
        "LOCUST_USERS=$Users",
        "LOCUST_SPAWN_RATE=$SpawnRate",
        "LOCUST_RUN_TIME=$RunTimeSeconds"
    )

    $commandArgs = @(
        "load", "test", "create",
        "--test-id", $TestId,
        "--load-test-resource", $LoadTestResourceName,
        "--resource-group", $ResourceGroupName,
        "--display-name", $DisplayName,
        "--description", $Description,
        "--test-type", "Locust",
        "--test-plan", $LocustFilePath,
        "--engine-instances", [string]$EngineInstances,
        "--engine-ref-id-type", "UserAssigned",
        "--engine-ref-ids", $EngineIdentityResourceId,
        "--metrics-reference-id", $MetricsReferenceIdentityResourceId,
        "--only-show-errors"
    )

    $commandArgs += "--env"
    $commandArgs += $envArgs

    Invoke-CliCommand -Action {
        az @commandArgs
    }
}

function Send-LoadTestFile {
    <#
	.SYNOPSIS
		Uploads a test file to Azure Load Testing through Azure CLI.
	#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$FileType
    )

    $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
    Invoke-CliCommand -Action {
        az load test file upload `
            --test-id $TestId `
            --load-test-resource $LoadTestResourceName `
            --resource-group $ResourceGroupName `
            --path $resolvedPath `
            --file-type $FileType `
            --only-show-errors
    }
}

function Get-ServerMetrics {
    <#
	.SYNOPSIS
		Returns the Application Insights server-side metrics to attach to the load test.
	#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ApplicationInsightsResourceId,

        [Parameter(Mandatory)]
        [object[]]$Metrics
    )

    foreach ($metric in $Metrics) {
        $metricNameSegment = [uri]::EscapeDataString([string]$metric.Name)
        [pscustomobject]@{
            MetricId         = "$ApplicationInsightsResourceId/providers/microsoft.insights/metricdefinitions/$metricNameSegment"
            MetricName       = $metric.Name
            MetricNamespace  = $metric.Namespace
            Aggregation      = $metric.Aggregation
            AppComponentId   = $ApplicationInsightsResourceId
            AppComponentType = "microsoft.insights/components"
        }
    }
}

function Add-ApplicationInsightsAppComponent {
    <#
	.SYNOPSIS
		Adds the Application Insights resource as a monitored app component for the load test.
	#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ApplicationInsightsResourceId
    )

    Invoke-CliCommand -Action {
        az load test app-component add `
            --test-id $TestId `
            --load-test-resource $LoadTestResourceName `
            --resource-group $ResourceGroupName `
            --app-component-id $ApplicationInsightsResourceId `
            --app-component-name $ApplicationInsightsResourceName `
            --app-component-type microsoft.insights/components `
            --app-component-kind web `
            --only-show-errors
    }
}

function Add-ServerMetrics {
    <#
	.SYNOPSIS
		Adds server-side Application Insights metrics to the Azure Load Testing test.
	#>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [object[]]$ServerMetrics
    )

    foreach ($metric in $ServerMetrics) {
        Invoke-CliCommand -Action {
            az load test server-metric add `
                --test-id $TestId `
                --load-test-resource $LoadTestResourceName `
                --resource-group $ResourceGroupName `
                --metric-id $($metric.MetricId) `
                --metric-name $($metric.MetricName) `
                --metric-namespace $($metric.MetricNamespace) `
                --aggregation $($metric.Aggregation) `
                --app-component-type $($metric.AppComponentType) `
                --app-component-id $($metric.AppComponentId) `
                --only-show-errors
        }
    }
}

# Define test properties and parameters

$TestId = "main-topic-locust"
$DisplayName = "Main Topic Locust Load Test"
$Description = "Publishes Event Hub events with Locust."
$LocustFilePath = (Resolve-Path -LiteralPath "$PSScriptRoot/../LoadTests/main_load_test.py").Path
$RequirementsFilePath = (Resolve-Path -LiteralPath "$PSScriptRoot/../LoadTests/requirements.txt").Path
$MetricDefinitions = @(
    @{
        Name        = "AsyncProcessingApp.EventLeadTimeMilliseconds"
        Namespace   = "azure.applicationinsights"
        Aggregation = "Average"
    },
    @{
        Name        = "AsyncProcessingApp.EventCycleTimeMilliseconds"
        Namespace   = "azure.applicationinsights"
        Aggregation = "Average"
    },
    @{
        Name        = "exceptions/count"
        Namespace   = "microsoft.insights/components"
        Aggregation = "Count"
    }
)
$EngineInstances = 1
$Users = 20
$SpawnRate = 5
$RunTimeSeconds = 900 # 15 minutes

$engineIdentityResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$LoadTestIdentityName"
$metricsReferenceIdentityResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.ManagedIdentity/userAssignedIdentities/$LoadTestIdentityName"
$applicationInsightsResourceId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Insights/components/$ApplicationInsightsResourceName"

[Array]$serverMetrics = @(Get-ServerMetrics -ApplicationInsightsResourceId $applicationInsightsResourceId -Metrics $MetricDefinitions)

Write-Host "Load test resource: $LoadTestResourceName"
Write-Host "Resource group: $ResourceGroupName"
Write-Host "Subscription ID: $SubscriptionId"
Write-Host "Test ID: $TestId"
Write-Host "Locust file: $LocustFilePath"
Write-Host "Requirements file: $RequirementsFilePath"
Write-Host "Engine identity: $engineIdentityResourceId"
Write-Host "Metrics identity: $metricsReferenceIdentityResourceId"
Write-Host "Application Insights resource: $applicationInsightsResourceId"

if (Test-LoadTestExists) {
    Remove-LoadTest
}

# Creating the load test involves multiple steps, including:
# - creating the test
# - uploading the Locust script and requirements file
# - adding the Application Insights component
# - adding server-side metrics.

Set-LoadTest -EngineIdentityResourceId $engineIdentityResourceId -MetricsReferenceIdentityResourceId $metricsReferenceIdentityResourceId
Send-LoadTestFile -Path $LocustFilePath -FileType "TEST_SCRIPT"
Send-LoadTestFile -Path $RequirementsFilePath -FileType "ADDITIONAL_ARTIFACTS"

Add-ApplicationInsightsAppComponent -ApplicationInsightsResourceId $applicationInsightsResourceId
Add-ServerMetrics -ServerMetrics $serverMetrics

Write-Host "Deployment of load test '$TestId' to '$LoadTestResourceName' completed successfully."
