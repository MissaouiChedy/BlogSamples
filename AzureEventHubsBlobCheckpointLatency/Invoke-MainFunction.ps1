param (
    [Parameter(Mandatory=$true)]
    [string]$FunctionUrl,
    $CallsCount = 100
)
1..$CallsCount `
    | ForEach-Object { 
        Invoke-RestMethod -Method POST `
            -Uri $FunctionUrl
        Start-Sleep -MilliSeconds 20
    }