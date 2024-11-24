$resourceGroupName = "rg-test-eventhub-send"
$region = "eastus"
$eventHubNamespace = "evh-test-sending"

## Create a Resource Group
az group create `
  --name $resourceGroupName `
  --location $region

## Create an Event Hub Namespace
az eventhubs namespace create `
  --name $eventHubNamespace `
  --resource-group $resourceGroupName `
  --location $region `
  --sku Standard `
  --capacity 1

## Create an Event Hub
az eventhubs eventhub create `
  --name "main-topic" `
  --namespace-name $eventHubNamespace `
  --resource-group $resourceGroupName `
  --cleanup-policy Delete `
  --retention-time 2 `
  --partition-count 1

## Grant Access to user to send messages on the EventHub
$eventHubId = az eventhubs eventhub show `
  --namespace-name $eventHubNamespace `
  --name "main-topic" `
  --resource-group $resourceGroupName `
  --query id `
  --output tsv

az role assignment create `
  --assignee "chedy.missaoui@<REDACTED_DOMAIN>" `
  --role "Azure Event Hubs Data Sender" `
  --scope $eventHubId
