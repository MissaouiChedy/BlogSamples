
<#
.SYNOPSIS
Generates a sample main-topic event payload as JSON.

.DESCRIPTION
Creates a random event object using valid AreaCode values from main-topic-event.schema.json.

.EXAMPLE
.\UtilityScripts\New-SampleEvent.ps1
#>

$schemaPath = Join-Path $PSScriptRoot "..\main-topic-event.schema.json"
$schema = Get-Content -Path $schemaPath -Raw | ConvertFrom-Json
$areaCodes = @($schema.properties.AreaCode.enum)

$nowUtc = [DateTimeOffset]::UtcNow

$content = Get-Random -Minimum 0 -Maximum 500
$areaCode = $areaCodes[(Get-Random -Minimum 0 -Maximum $areaCodes.Count)]

$eventPayload = [ordered]@{
	Id        = [Guid]::NewGuid().ToString()
	Content   = $content
	AreaCode  = $areaCode
	CreatedAt = $nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
	SentAt    = $nowUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
}

$eventPayload | ConvertTo-Json
