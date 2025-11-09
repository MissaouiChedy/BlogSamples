using Microsoft.Azure.Cosmos;
using Azure.Identity;
using IncidentAgent.Models;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IResolutionService
    {
        Task<Resolution?> GetResolutionByTicketIdAsync(string ticketId);
    }

    public class CosmosDbResolutionService : IResolutionService
    {
        private readonly Container _container;

        public CosmosDbResolutionService(IConfiguration configuration)
        {
            var endpoint = configuration["CosmosDb:Endpoint"] ??
                throw new ArgumentNullException("CosmosDb:Endpoint configuration is missing");
            var databaseId = configuration["CosmosDb:DatabaseId"] ??
                throw new ArgumentNullException("CosmosDb:DatabaseId configuration is missing");
            var containerId = "Resolutions";
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeManagedIdentityCredential = false
            });
            var cosmosClient = new CosmosClient(endpoint, credential);
            _container = cosmosClient.GetContainer(databaseId, containerId);
        }

        public async Task<Resolution?> GetResolutionByTicketIdAsync(string ticketId)
        {
            var query = _container.GetItemQueryIterator<Resolution>(
                new QueryDefinition("SELECT * FROM c WHERE c.TicketId = @ticketId")
                    .WithParameter("@ticketId", ticketId));
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                var res = response.FirstOrDefault();
                if (res != null)
                    return res;
            }
            return null;
        }
    }
}
