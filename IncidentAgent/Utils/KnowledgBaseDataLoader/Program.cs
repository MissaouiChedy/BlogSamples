
using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

const string sampleDataFile = "sample.json";

var jsonContent = await File
    .ReadAllTextAsync(sampleDataFile);

var entries = JsonSerializer.Deserialize<List<KnowledgeBaseEntry>>(jsonContent);


var endpoint = "https://cosmos-ticket-classification-f1cc.documents.azure.com:443/";
var databaseId = "TicketDB";
var containerId = "KnowledgeBase";
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeVisualStudioCredential = false,
    ExcludeAzureCliCredential = false,
    ExcludeManagedIdentityCredential = false,
});
var cosmosClient = new CosmosClient(endpoint, credential);
var container = cosmosClient.GetContainer(databaseId, containerId);


foreach (var entry in entries)
{
    await container
        .CreateItemAsync(entry, new PartitionKey(entry.Category));
}

Console.WriteLine("Data loading completed.");