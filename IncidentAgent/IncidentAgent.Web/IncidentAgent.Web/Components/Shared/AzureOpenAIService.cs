using Azure.Identity;
using IncidentAgent.Models;
using IncidentAgent.Web.Components.Configuration;
using Microsoft.Extensions.Options;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Microsoft.Agents.AI;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IAzureOpenAIService
    {
        Task<Ticket> GenerateTicketFromDescriptionAsync(string description);
    }

    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly string _agentName = "TicketStructureAgent";
        private readonly string _systemMessage = """
             You are a helpful assistant that creates support tickets from descriptions. 
             Create a well-formatted title that summarizes the issue.
             Create a description that elaborates the issue with expert language
             Return ONLY a valid JSON object with the following fields: Title, Description, Category
             Category must be one of: Hardware, Software, Network or Security
             Do not include any explanation, markdown, or text outside the JSON object.
            """;
        private readonly ChatClientAgent _agent;
        
        public AzureOpenAIService(IOptions<AzureOpenAISettings> azureOptions)
        {
            var settings = azureOptions.Value;
            var endpoint = settings.Endpoint;
            var deploymentName = settings.DeploymentName;

            _agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                .AsAIAgent(model: deploymentName,
                    instructions: _systemMessage,
                    name: _agentName);
        }

        public async Task<Ticket> GenerateTicketFromDescriptionAsync(string description)
        {
            var chatResponse = await _agent.RunAsync<Ticket>(description);
            return chatResponse.Result;
        }
    }
}