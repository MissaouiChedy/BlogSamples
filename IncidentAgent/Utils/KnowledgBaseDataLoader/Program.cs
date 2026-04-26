
using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

const string sampleDataFile = "sample.json";

var jsonContent = await File
    .ReadAllTextAsync(sampleDataFile);

var entries = JsonSerializer.Deserialize<List<KnowledgeBaseEntry>>(jsonContent);

const string defaultEndpoint = "https://cosmos-ticket-classification-a8a2.documents.azure.com:443/";
var endpoint = GetCosmosEndpointFromTerraformStateOrDefault(defaultEndpoint);
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

static string GetCosmosEndpointFromTerraformStateOrDefault(string fallbackEndpoint)
{
    var terraformStatePath = FindTerraformStatePath();
    if (terraformStatePath is null)
    {
        return fallbackEndpoint;
    }

    try
    {
        using var stream = File.OpenRead(terraformStatePath);
        using var document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("resources", out var resources) || resources.ValueKind != JsonValueKind.Array)
        {
            return fallbackEndpoint;
        }

        foreach (var resource in resources.EnumerateArray())
        {
            if (!resource.TryGetProperty("type", out var type) ||
                !string.Equals(type.GetString(), "azurerm_cosmosdb_account", StringComparison.Ordinal))
            {
                continue;
            }

            if (!resource.TryGetProperty("instances", out var instances) || instances.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var instance in instances.EnumerateArray())
            {
                if (!instance.TryGetProperty("attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (!attributes.TryGetProperty("endpoint", out var endpointProperty))
                {
                    continue;
                }

                var endpoint = endpointProperty.GetString();
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    return endpoint;
                }
            }
        }
    }
    catch (IOException)
    {
        return fallbackEndpoint;
    }
    catch (JsonException)
    {
        return fallbackEndpoint;
    }

    return fallbackEndpoint;
}

static string? FindTerraformStatePath()
{
    var current = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (current is not null)
    {
        var candidatePath = Path.Combine(current.FullName, "azure-resources", "terraform.tfstate");
        if (File.Exists(candidatePath))
        {
            return candidatePath;
        }

        current = current.Parent;
    }

    return null;
}