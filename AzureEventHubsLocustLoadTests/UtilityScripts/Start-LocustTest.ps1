<#
.SYNOPSIS
Starts the Locust load test for the Event Hubs producer scenario in this repository.

.DESCRIPTION
Activates the Python virtual environment under LoadTests/.venv, runs Locust with the
main_load_test.py test plan, and optionally executes in headless mode for CI or CLI runs.

This command is intended to be run from anywhere; it resolves paths relative to this script.

.PARAMETER Users
Total number of concurrent Locust users to simulate.

.PARAMETER SpawnRate
Number of users spawned per second until the target user count is reached.

.PARAMETER RunTimeSeconds
Total test duration in seconds.

.PARAMETER EventHubNamespace
Event Hubs namespace FQDN.
Currently accepted for command compatibility and future wiring, but not directly passed
to Locust arguments by this script.

.PARAMETER EventHubName
Event Hub name.
Currently accepted for command compatibility and future wiring, but not directly passed
to Locust arguments by this script.

.PARAMETER Headless
Runs Locust in headless mode (no web UI).

.EXAMPLE
./UtilityScripts/Start-LocustTest.ps1

Runs with defaults: 10 users, spawn rate 5 users/sec, 60 second duration.

.EXAMPLE
./UtilityScripts/Start-LocustTest.ps1 -Users 100 -SpawnRate 20 -RunTimeSeconds 180 -Headless

Runs a 3-minute headless test with 100 users and a spawn rate of 20 users/sec.

.EXAMPLE
./UtilityScripts/Start-LocustTest.ps1 -EventHubNamespace "evh-test-main-eventhub-ns-463e.servicebus.windows.net" -EventHubName "main-topic"

Provides explicit Event Hubs identifiers while running the Locust scenario.
#>
[CmdletBinding()]
param(
    [int]$Users = 5,
    [int]$SpawnRate = 5,
    [int]$RunTimeSeconds = 60,
    [string]$EventHubNamespace = "evh-test-main-eventhub-ns-463e.servicebus.windows.net",
    [string]$EventHubName = "main-topic",
    [switch]$Headless
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$loadTestsDir = Join-Path $repoRoot "LoadTests"
$venvActivatePath = Join-Path $loadTestsDir ".venv\Scripts\Activate.ps1"
$locustFileName = "main_load_test.py"

try {
    Push-Location
    Set-Location $loadTestsDir
    
    # Load virtual environment for this PowerShell session.
    . $venvActivatePath

    $locustArgs = @(
        "-m", "locust",
        "-f", $locustFileName,
        "--users", $Users,
        "--spawn-rate", $SpawnRate,
        "--run-time", "${RunTimeSeconds}s"
    )

    if ($Headless.IsPresent) {
        $locustArgs += "--headless"
    }

    & python @locustArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Locust exited with code $LASTEXITCODE."
    }
}
finally {
    # Explicitly unload venv from the current shell if deactivate is available.
    if (Get-Command deactivate -ErrorAction SilentlyContinue) {
        deactivate
    }

    Pop-Location
}
