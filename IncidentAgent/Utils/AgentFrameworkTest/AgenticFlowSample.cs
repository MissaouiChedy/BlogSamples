using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.Text;
using System.Text.Json;

namespace AgentFrameworkTest
{
    public static class AgenticFlowSample
    {
        public static async Task Run()
        {

            AzureOpenAIClient client = new(
                new Uri("https://aif-main-foundry-f1cc.cognitiveservices.azure.com"),
                new DefaultAzureCredential());

            await using McpClient kbTools = await McpClient
                .CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
                {
                    TransportMode = HttpTransportMode.StreamableHttp,
                    Endpoint = new Uri("https://app-mcp-server-chk-f1cc.azurewebsites.net/api/mcp"),
                }));

            //https://app-mcp-server-chk-f1cc.azurewebsites.net
            
            IList<McpClientTool> toolsInKBMcp = await kbTools.ListToolsAsync();

            var resolutionAgent = client
                .GetChatClient("incident-classification-gpt41")
                .CreateAIAgent(
                    name: "ResolutionAgent",
                    instructions: @"You are a support engineer. 
        Given a ticket description, generate a step-by-step resolution plan as an array of strings.
        When presented with an issue with an unkown system you collaborate with other Agents to get info from a knowledge base");

            var knowledgeBaseManagerAgent = client
                .GetChatClient("incident-classification-gpt41")
                .CreateAIAgent(
                    name: "KnowledgeBaseManagerAgent",
                    instructions: @"You are a Kablam Company Knowledge Base expert.
                    When presented with and issue, search for the issue in the kablam company knowledge base
                    If an issue has been asked about with no existing entry in the knowledbase, add a new entry in the knowledge base about it.
                    You do not generate resolution steps
                    Knowledge base entries should have the following format: (Title, Issue, Solution, Discussion)
                    After determining a knowledge base entry, handoff back to the Resolution Agent to generate resolution steps.
",

                    tools: toolsInKBMcp.Cast<AITool>().ToList())
                .AsBuilder()
                .Use(FunctionCallMiddleware)
                .Build();

            Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(resolutionAgent)
                .WithHandoff(resolutionAgent, knowledgeBaseManagerAgent)
                .WithHandoff(knowledgeBaseManagerAgent, resolutionAgent)
                .Build();

            List<ChatMessage> messages = [];

            var message = "The MELTCORE 6 inventory management system is not reflecting updated stock quantities after new purchase orders are added. Despite successfully completing several purchase orders, the inventory levels in the Inventory module remain unchanged. It is unclear whether the system should update stock levels automatically or if a manual refresh (such as using the 'Sync' option) is required. The user requests verification of system functionality and clarification of the correct process to ensure inventory data is accurately updated.";
            messages.Add(new(ChatRole.User, message));
            await RunWorkflowAsync(workflow, messages);
        }

        public static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
        {
            string? lastExecutorId = null;

            StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            await foreach (WorkflowEvent @event in run.WatchStreamAsync())
            {
                switch (@event)
                {
                    case AgentRunUpdateEvent e:
                        {
                            if (e.ExecutorId != lastExecutorId)
                            {
                                lastExecutorId = e.ExecutorId;
                                Console.WriteLine();
                                Console.WriteLine($">>>>>> {e.Update.AuthorName ?? e.ExecutorId}");
                            }

                            Console.Write(e.Update.Text);
                            if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                            {
                                Console.WriteLine();
                                Console.WriteLine($">>>>>> Call '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                            }

                            break;
                        }
                    case WorkflowOutputEvent output:
                        Console.WriteLine();
                        Console.WriteLine("-------------------------");
                        return output.As<List<ChatMessage>>()!;
                }
            }

            return [];
        }

        public static async ValueTask<object?> FunctionCallMiddleware(AIAgent callingAgent, FunctionInvocationContext context, Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next, CancellationToken cancellationToken)
        {
            StringBuilder functionCallDetails = new();
            functionCallDetails.Append($">>>>>>> - Tool Call: '{context.Function.Name}'");
            if (context.Arguments.Count > 0)
            {
                functionCallDetails.Append($" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))}");
            }

            Console.WriteLine(functionCallDetails.ToString());

            return await next(context, cancellationToken);
        }
    }
}
