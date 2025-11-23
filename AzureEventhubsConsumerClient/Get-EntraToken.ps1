# Get the current authenticated user principal id
$userPrincipalId = az ad signed-in-user show --query id --output tsv

# Generate an access token for the user
$accessToken = az account get-access-token --resource 'https://redis.azure.com' --query accessToken --output tsv

# Output the user principal id and access token
Write-Output "---"
Write-Output "User Principal ID: $userPrincipalId"
Write-Output "---"
Write-Output "Token: $accessToken"
Write-Output "---"
