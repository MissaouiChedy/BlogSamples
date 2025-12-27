param (
    [string]$FunctionUrl="https://mainfuncapplatencyb74b.azurewebsites.net/api/MainLatencyMeasurementFunction?code=<REDACTED>",
    $CallsCount = 100
)
1..$CallsCount `
    | ForEach-Object { 
        Invoke-RestMethod -Method POST `
            -Uri $FunctionUrl
        Start-Sleep -MilliSeconds 20
    }