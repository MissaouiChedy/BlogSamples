using Azure.Identity;
using IncidentAgent.Models;
using IncidentAgent.Web.Components.Configuration;
using Microsoft.Extensions.Options;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IAzureOpenAIService
    {
        Task<Ticket> GenerateTicketFromDescriptionAsync(string description);
    }

    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly string _agentName = "TicketStructureAgent";
        private readonly string _systemMessage = @"You are a helpful assistant that creates support tickets from descriptions. 
                Create a well-formatted title that summarizes the issue.
                Create a description that elaborates the issue with expert language
                Return ONLY a valid JSON object with the following fields: Title, Description, Category
                Category must be one of: Hardware, Software, Network or Security
                Do not include any explanation, markdown, or text outside the JSON object.
                ";
        private readonly ProjectsAgentVersion _agentVersion;
        private readonly AIProjectClient _client;
        private readonly string _deploymentName;
        
        public AzureOpenAIService(IOptions<AzureOpenAISettings> azureOptions)
        {
            var settings = azureOptions.Value;
            var endpoint = settings.Endpoint;
            _deploymentName = settings.DeploymentName;

            _client = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

            var agentInfo = _client
                .AgentAdministrationClient
                .GetAgents()
                .FirstOrDefault(a => a.Name == _agentName);
            
            if (agentInfo is null)
            {
                DeclarativeAgentDefinition definition = new(_deploymentName)
                {
                    Instructions = _systemMessage,
                    Temperature = 0.4f,
                };
                _agentVersion = _client
                    .AgentAdministrationClient
                    .CreateAgentVersion(_agentName, new ProjectsAgentVersionCreationOptions(definition)
                    {
                        Description = "Support Ticket structuring agent",
                    })
                    .Value;
            }
            else
            {
                _agentVersion = agentInfo.GetLatestVersion();
            }
        }

        public async Task<Ticket> GenerateTicketFromDescriptionAsync(string description)
        {
            var agent = _client.AsAIAgent(_agentVersion);

            var session = await agent.CreateSessionAsync();

            var chatResponse = await agent.RunAsync<Ticket>(description, session);

            return chatResponse.Result;
        }
    }
}