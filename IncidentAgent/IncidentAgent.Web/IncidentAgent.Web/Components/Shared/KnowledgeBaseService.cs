using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Net;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IKnowledgeBaseService
    {
        Task<List<KnowledgeBaseEntry>> GetKnowledgeBaseEntries();
        Task<KnowledgeBaseEntry?> GetKnowledgeBaseEntry(string id);
    }
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly Container _container;

        public KnowledgeBaseService(IConfiguration configuration)
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

        public async Task<List<KnowledgeBaseEntry>> GetKnowledgeBaseEntries()
        {
            var query = _container.GetItemQueryIterator<KnowledgeBaseEntry>(
                new QueryDefinition("SELECT TOP 100 * FROM c")
                );

            var results = new List<KnowledgeBaseEntry>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<KnowledgeBaseEntry?> GetKnowledgeBaseEntry(string id)
        {
            try
            {
                var query = _container.GetItemQueryIterator<KnowledgeBaseEntry>(
                new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id));
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    var res = response.FirstOrDefault();
                    if (res != null)
                        return res;
                }
                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
