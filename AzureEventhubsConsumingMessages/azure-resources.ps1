$resourceGroupName = "rg-test-eventhub-consume"
$region = "eastus"
$eventHubNamespace = "evh-test-consuming"
$eventhubTopicName = "main-topic"
$eventhubConsumerGroupName= "main-consumer"
$storageAccountName = "mainconsumerstorageacc"
$storageContainerName = "main-consumer"
$userEmail = "chedy.missaoui@<REDACTED>"

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
  --name $eventhubTopicName `
  --namespace-name $eventHubNamespace `
  --resource-group $resourceGroupName `
  --cleanup-policy Delete `
  --retention-time 2 `
  --partition-count 1

az eventhubs eventhub consumer-group create `
  --consumer-group-name $eventhubConsumerGroupName `
  --eventhub-name $eventhubTopicName `
  --namespace-name $eventHubNamespace `
  --resource-group $resourceGroupName

az storage account create --name $storageAccountName `
  --resource-group $resourceGroupName `
  --access-tier 'Hot' `
  --sku "Standard_LRS" `
  --allow-blob-public-access true

az storage container create --name $storageContainerName `
  --account-name $storageAccountName `
  --auth-mode login

## Grant Access to user Eventhub and Storage Resources
$eventHubId = az eventhubs eventhub show `
  --namespace-name $eventHubNamespace `
  --name $eventhubTopicName `
  --resource-group $resourceGroupName `
  --query id `
  --output tsv

$storageAccountId = az storage account show `
  --resource-group $resourceGroupName `
  --name $storageAccountName `
  --query id `
  --output tsv

az role assignment create `
  --assignee $userEmail `
  --role "Azure Event Hubs Data Sender" `
  --scope $eventHubId

az role assignment create `
  --assignee $userEmail `
  --role "Azure Event Hubs Data Receiver" `
  --scope $eventHubId

az role assignment create --assignee $userEmail `
  --role "Storage Blob Data Contributor" `
  --scope $storageAccountId

