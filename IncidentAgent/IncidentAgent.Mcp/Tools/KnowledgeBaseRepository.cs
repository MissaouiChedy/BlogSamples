using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using Azure.Core;

namespace IncidentAgent.Mcp.Tools
{
    public class KnowledgeBaseRepository
    {
        private readonly Container _container;

        public KnowledgeBaseRepository(IOptions<CosmosDBConfiguration> cosmosDbOptions)
        {
            CosmosDBConfiguration cosmosConfig = cosmosDbOptions.Value;
            TokenCredential credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions());
            CosmosClient cosmosClient = new CosmosClient(cosmosConfig.Endpoint, credential);
            _container = cosmosClient.GetContainer(cosmosConfig.DatabaseId, cosmosConfig.ContainerId);
        }

        public async Task<KnowledgeBaseEntry> AddKnowledgeBaseEntry(KnowledgeBaseEntry entry)
        {
            entry.Id = Guid.NewGuid().ToString();
            var response = await _container.CreateItemAsync(entry, new PartitionKey(entry.Category));
            return response.Resource;
        }

        public async Task<List<KnowledgeBaseEntry>> SearchKnowledgeBaseEntries(string searchQuery)
        {
            string queryWords = string.Join(",", Regex
                .Split(searchQuery, @"\s+")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => $"'{w}'")
                .ToArray());

            var query = new QueryDefinition(
                @$"SELECT * FROM c WHERE
                FullTextContainsAny(c.Title, {queryWords}) OR
                FullTextContainsAny(c.Issue, {queryWords}) OR
                FullTextContainsAny(c.Solution, {queryWords}) OR
                FullTextContainsAny(c.Discussion, {queryWords})");

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
