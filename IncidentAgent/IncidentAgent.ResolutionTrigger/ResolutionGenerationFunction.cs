using IncidentAgent.Models;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using OpenAI;
using System.Text;
using System.Text.Json;
using HttpClientTransport = ModelContextProtocol.Client.HttpClientTransport;

namespace IncidentAgent.ResolutionTrigger
{
    public class ResolutionGenerationFunction
    {
        private readonly ILogger _logger;
        private readonly string _deploymentName;
        private readonly string _knowledgeBaseMcpServerUrl;
        private readonly AzureOpenAIClient _client;

        public ResolutionGenerationFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ResolutionGenerationFunction>();

            var endpoint = Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint")
                ?? throw new ArgumentNullException("AzureOpenAI__Endpoint env var is missing");
            _deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName")
                ?? throw new ArgumentNullException("AzureOpenAI__DeploymentName env var is missing");
            _knowledgeBaseMcpServerUrl = Environment.GetEnvironmentVariable("AzureOpenAI__KnowledgeBaseMCPServerUrl")
                ?? throw new ArgumentNullException("KnowledgeBaseMcpServerUrl env var is missing");

            var credential = new DefaultAzureCredential();

            _client = new(
                new Uri(endpoint),
                credential);
        }

        [Function("Function1")]
        [CosmosDBOutput("TicketDB", "Resolutions",
            Connection = "COSMOS_CONNECTION",
            CreateIfNotExists = false)]
        public async Task<object> Run([CosmosDBTrigger(
            databaseName: "TicketDB",
            containerName: "Tickets",
            Connection = "COSMOS_CONNECTION",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = false)] IReadOnlyList<Ticket> input)
        {

            _logger.LogInformation("Running Functions");
            var resolutions = new List<Resolution>();

            if (input != null && input.Count > 0)
            {
                _logger.LogInformation($"Documents modified: {input.Count}");

                foreach (var ticket in input)
                {
                    _logger.LogInformation($"Processing ticket: {ticket.Id}");


                    string[] steps = await GenerateResolutionForTicket(ticket);

                    var resolution = new Resolution
                    {
                        TicketId = ticket.Id,
                        Status = "Pending",
                        Steps = steps,
                        Category = ticket.Category
                    };

                    resolutions.Add(resolution);
                    _logger.LogInformation($"Created resolution {resolution.id} for ticket {ticket.Id}");
                }
            }
            else
            {
                _logger.LogInformation("No Documents to process");
            }

            return resolutions;
        }

        private async Task<string[]> GenerateResolutionForTicket(Ticket ticket)
        {
            await using McpClient kbTools = await McpClient
                .CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
                {
                    TransportMode = HttpTransportMode.StreamableHttp,
                    Endpoint = new Uri(_knowledgeBaseMcpServerUrl),
                }));
            IList<McpClientTool> toolsInKBMcp = await kbTools.ListToolsAsync();

            var resolutionAgent = _client
                .GetChatClient(_deploymentName)
                .CreateAIAgent(
                    name: "KnowledgeBaseManagerAgent",
                    instructions: @"You are a support engineer. 
                    Given a ticket description, generate a step-by-step resolution plan as a JSON array of strings.
                    When presented with an issue about a KABLAM Company system search the knowledge base for existing solutions
                    When presented with an issue about a KABLAM Company system not available in the knowledge base add a new entry in the knowledge base",
                    tools: toolsInKBMcp.Cast<AITool>().ToList())
                .AsBuilder()
                .Use(FunctionCallMiddleware)
                .Build();
            var response = await resolutionAgent.RunAsync($"{ticket.Title} {ticket.Category} {ticket.Description} ");

            return JsonSerializer
                .Deserialize<string[]>(response.ToString())
                ?? [];
        }

        private async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
        {
            StringBuilder functionCallDetails = new();
            functionCallDetails.Append($"Tool Call: '{context.Function.Name}'");
            if (context.Arguments.Count > 0)
            {
                functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
            }

            _logger.LogInformation(functionCallDetails.ToString());

            return await next(context, cancellationToken);
        }
    }
}
