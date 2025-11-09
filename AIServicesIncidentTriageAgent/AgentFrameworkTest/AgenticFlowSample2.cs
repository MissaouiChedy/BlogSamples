using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using Newtonsoft.Json;
using OpenAI;
using System.Text;

namespace AgentFrameworkTest
{
    public static class AgenticFlowSample2
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
        When presented with an issue about a KABLAM Company system search the knowledge base for existing solutions
        When presented with an issue about a KABLAM Company system not available in the knowledge base add a new entry in the knowledge base",
                    tools: toolsInKBMcp.Cast<AITool>().ToList())
                .AsBuilder()
                .Use(FunctionCallMiddleware)
                .Build(); ;

            //var message = "The MELTCORE 6 inventory management system is not reflecting updated stock quantities after new purchase orders are added. Despite successfully completing several purchase orders, the inventory levels in the Inventory module remain unchanged. It is unclear whether the system should update stock levels automatically or if a manual refresh (such as using the 'Sync' option) is required. The user requests verification of system functionality and clarification of the correct process to ensure inventory data is accurately updated.";
            var message = "Following a recent update, the MELTCORE 6 system has ceased sending automated email notifications for key events such as purchase order approvals and invoice generation. Users have verified that emails are not being received by any team members, and there are no indications of errors within the application interface. Spam filters and mail server configurations appear to be correctly set, suggesting the issue may be internal to MELTCORE 6, potentially involving a background service or configuration change post-update. Further investigation is required to determine if a service restart or configuration adjustment is needed to restore email functionality.";
            var response = await resolutionAgent.RunAsync(message);

            Console.WriteLine("=================");
            Console.WriteLine(response.Text);
            Console.WriteLine("=================");

            var xs = System.Text.Json.JsonSerializer.Deserialize<string[]>(response.Text);
            foreach (var x in xs!)
            {
                Console.WriteLine($"- {x}");
            }
        }

        //public static async Task<List<ChatMessage>> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
        //{
        //    string? lastExecutorId = null;

        //    StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
        //    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        //    await foreach (WorkflowEvent @event in run.WatchStreamAsync())
        //    {
        //        switch (@event)
        //        {
        //            case AgentRunUpdateEvent e:
        //                {
        //                    if (e.ExecutorId != lastExecutorId)
        //                    {
        //                        lastExecutorId = e.ExecutorId;
        //                        Console.WriteLine();
        //                        Console.WriteLine($">>>>>> {e.Update.AuthorName ?? e.ExecutorId}");
        //                    }

        //                    Console.Write(e.Update.Text);
        //                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
        //                    {
        //                        Console.WriteLine();
        //                        Console.WriteLine($">>>>>> Call '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
        //                    }

        //                    break;
        //                }
        //            case WorkflowOutputEvent output:
        //                Console.WriteLine();
        //                Console.WriteLine("-------------------------");
        //                return output.As<List<ChatMessage>>()!;
        //        }
        //    }

        //    return [];
        //}

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
