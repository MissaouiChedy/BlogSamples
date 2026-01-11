using IncidentAgent.Models;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using System.Net;
using Microsoft.Extensions.Options;
using IncidentAgent.Web.Components.Configuration;

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

        public KnowledgeBaseService(IOptions<CosmosDbSettings> cosmosOptions)
        {
            var settings = cosmosOptions.Value;
            var endpoint = settings.Endpoint;
            var databaseId = settings.DatabaseId;
            var containerId = settings.KnowledgeBaseContainerId;
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
