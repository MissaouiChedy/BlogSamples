using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentFrameworkTest
{
    public class AzureOpenAISample
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
                Endpoint = new Uri("http://localhost:5081/api/mcp"),
            }));
            //https://app-mcp-server-chk-f1cc.azurewebsites.net
            IList<McpClientTool> toolsInGitHubMcp = await kbTools.ListToolsAsync();

            AIAgent agent = client
                .GetChatClient("incident-classification-gpt41")
                .CreateAIAgent(
                    instructions: "You are a Kablam Company Expert",
                    tools: toolsInGitHubMcp.Cast<AITool>().ToList()
                )
                .AsBuilder()
                .Use(FunctionCallMiddleware)
                .Build();

            AgentThread thread = agent.GetNewThread();

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                ChatMessage message = new(ChatRole.User, input);
                AgentRunResponse response = await agent.RunAsync(message, thread);

                Console.WriteLine(response);
            }
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
