using IncidentAgent.Models;
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

namespace IncidentAgent.Web.Components.Shared
{
    public interface IAzureOpenAIService
    {
        Task<Models.Ticket> GenerateTicketFromDescriptionAsync(string description);
    }

    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly string _agentName = "TicketStructureAgent";
        private readonly string _systemMessage = @"You are a helpful assistant that creates support tickets from descriptions. 
                Create a well-formatted title that summarizes the issue.
                Create a description that elaborates the issue with expert language
                Return a structured response with the following fields only: Title, Description, Category
                Category should be one of Hardware, Software, Network or Security
                ";
        private readonly PersistentAgent _agentInfo;
        private readonly PersistentAgentsClient _client;
        private readonly string _deploymentName;
        
        public AzureOpenAIService(IConfiguration configuration)
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"] ??
                throw new ArgumentNullException("AzureOpenAI:Endpoint configuration is missing");

            _deploymentName = configuration["AzureOpenAI:DeploymentName"] ??
                throw new ArgumentNullException("AzureOpenAI:DeploymentName configuration is missing");

            _client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());

            PersistentAgent? agentInfo = _client.Administration
                .GetAgents()
                .FirstOrDefault(a => a.Name == _agentName);
            
            if (agentInfo is null)
            {
                Response<PersistentAgent> aiFoundryAgent = _client.Administration.CreateAgent(
                    _deploymentName,
                    _agentName,
                    "Support Ticket structuring agent",
                    _systemMessage,
                    temperature: 0.4f
                );
                agentInfo = aiFoundryAgent.Value;
            }
            _agentInfo = agentInfo;
        }

        public async Task<Models.Ticket> GenerateTicketFromDescriptionAsync(string description)
        {
            var agent = await _client.GetAIAgentAsync(_agentInfo.Id);

            var thread = agent.GetNewThread();


            var chatResponse = await agent.RunAsync<Ticket>(description, thread);

            return chatResponse.Result;
        }
    }
}