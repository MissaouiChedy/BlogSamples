using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Text.RegularExpressions;

namespace IncidentAgent.Mcp.Tools
{
    public class KnowledgeBaseRepository
    {
        private readonly Container _container;

        public KnowledgeBaseRepository(IConfiguration configuration)
        {
            var endpoint = configuration["CosmosDb:Endpoint"] ??
                throw new ArgumentNullException("CosmosDb:Endpoint configuration is missing");
            var databaseId = configuration["CosmosDb:DatabaseId"] ??
                throw new ArgumentNullException("CosmosDb:DatabaseId configuration is missing");
            var containerId = "KnowledgeBase";
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeManagedIdentityCredential = false,
            });
            var cosmosClient = new CosmosClient(endpoint, credential);
            _container = cosmosClient.GetContainer(databaseId, containerId);
        }

        public async Task<KnowledgeBaseEntry> AddKnowledgeBaseEntry(KnowledgeBaseEntry entry)
        {
            entry.Id = Guid.NewGuid().ToString();
            var response = await _container.CreateItemAsync(entry, new PartitionKey(entry.Category));
            return response.Resource;
        }

        public async Task<List<KnowledgeBaseEntry>> SearchKnowledgeBaseEntries(string searchQuery)
        {
            string words = string.Join(",", Regex
                .Split(searchQuery, @"\s+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => $"'{w}'")
                .ToArray());

            var query = new QueryDefinition(
                @$"SELECT * FROM c WHERE
                FullTextContainsAny(c.Title, {words}) OR
                FullTextContainsAny(c.Issue, {words}) OR
                FullTextContainsAny(c.Solution, {words}) OR
                FullTextContainsAny(c.Discussion, {words})");

            var iterator = _container.GetItemQueryIterator<KnowledgeBaseEntry>(query);
            var results = new List<KnowledgeBaseEntry>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }
            return results;
        }
    }
}
