using Microsoft.Azure.Cosmos;
using Azure.Identity;
using IncidentAgent.Models;
using Microsoft.Extensions.Options;
using IncidentAgent.Web.Components.Configuration;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IResolutionService
    {
        Task<Resolution?> GetResolutionByTicketIdAsync(string ticketId);
    }

    public class CosmosDbResolutionService : IResolutionService
    {
        private readonly Container _container;

        public CosmosDbResolutionService(IOptions<CosmosDbSettings> cosmosOptions)
        {
            var settings = cosmosOptions.Value;
            var endpoint = settings.Endpoint;
            var databaseId = settings.DatabaseId;
            var containerId = settings.ResolutionsContainerId;
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
