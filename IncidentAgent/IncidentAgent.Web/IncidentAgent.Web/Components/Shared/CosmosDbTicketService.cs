using Microsoft.Azure.Cosmos;
using Azure.Identity;
using IncidentAgent.Models;
using Microsoft.Extensions.Options;
using IncidentAgent.Web.Components.Configuration;

namespace IncidentAgent.Web.Components.Shared
{
    public interface ITicketService
    {
        Task<IEnumerable<Ticket>> GetAllTicketsAsync();
        Task<Ticket> AddTicketAsync(Ticket ticket);
        Task<Ticket?> GetTicketAsync(string id);
    }

    public class CosmosDbTicketService : ITicketService
    {
        private readonly Container _container;

        public CosmosDbTicketService(IOptions<CosmosDbSettings> cosmosOptions)
        {
            var settings = cosmosOptions.Value;
            var endpoint = settings.Endpoint;
            var databaseId = settings.DatabaseId;
            var containerId = settings.TicketContainerId;
         
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeVisualStudioCredential = false,
                ExcludeAzureCliCredential = false,
                ExcludeManagedIdentityCredential = false
            });

            var cosmosClient = new CosmosClient(endpoint, credential);
            _container = cosmosClient.GetContainer(databaseId, containerId);
        }

        public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
        {
            var query = _container.GetItemQueryIterator<Ticket>(
                new QueryDefinition("SELECT * FROM c WHERE c.type = 'ticket'"));

            var results = new List<Ticket>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<Ticket> AddTicketAsync(Ticket ticket)
        {
            var response = await _container.CreateItemAsync(ticket, new PartitionKey(ticket.Category));
            return response.Resource;
        }

        public async Task<Ticket?> GetTicketAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Ticket>(id, new PartitionKey("ticket"));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}