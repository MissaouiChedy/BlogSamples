<#
.SYNOPSIS
    Removes all files and subfolders inside PublishProfiles and ServiceDependencies folders.

.DESCRIPTION
    Recursively scans the current solution folder (script root by default) for folders
    named PublishProfiles or ServiceDependencies and deletes only their contents.
    The folders themselves are preserved.

.PARAMETER SolutionRoot
    Root folder to scan. Defaults to the folder containing this script.

.EXAMPLE
    .\Clear-PublishAndServiceDependencies.ps1

.EXAMPLE
    .\Clear-PublishAndServiceDependencies.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$SolutionRoot = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SolutionRoot -PathType Container)) {
    throw "SolutionRoot '$SolutionRoot' does not exist or is not a directory."
}

$targetFolderNames = @('PublishProfiles', 'ServiceDependencies')
$targetFolders = Get-ChildItem -LiteralPath $SolutionRoot -Directory -Recurse -Force |
    Where-Object { $_.Name -in $targetFolderNames }

if (-not $targetFolders) {
    Write-Host "No PublishProfiles or ServiceDependencies folders found under '$SolutionRoot'." -ForegroundColor Yellow
    return
}

Write-Host "Found $($targetFolders.Count) target folder(s)." -ForegroundColor Cyan

$removedCount = 0
foreach ($folder in $targetFolders) {
    Write-Host "Processing: $($folder.FullName)" -ForegroundColor Cyan

    $children = Get-ChildItem -LiteralPath $folder.FullName -Force
    if (-not $children) {
        Write-Host "  Already empty." -ForegroundColor DarkGray
        continue
    }

    foreach ($child in $children) {
        if ($PSCmdlet.ShouldProcess($child.FullName, 'Remove item')) {
            Remove-Item -LiteralPath $child.FullName -Recurse -Force
            $removedCount++
            Write-Host "  Removed: $($child.Name)" -ForegroundColor Green
        }
    }
}

Write-Host "Completed. Removed $removedCount item(s)." -ForegroundColor Green
