using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using OpenAI;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AgentFrameworkTest
{
    public static class LocalLLMAgent
    {
        public static async Task  Run()
        {
            string modelAlias = "qwen2.5-0.5b";

            FoundryLocalManager? manager = await FoundryLocalManager
                .StartModelAsync(aliasOrModelId: modelAlias);

            ModelInfo? model = await manager
                .GetModelInfoAsync(aliasOrModelId: modelAlias);

            OpenAIClient client = new OpenAIClient(new ApiKeyCredential(manager.ApiKey), new OpenAIClientOptions
            {
                Endpoint = manager.Endpoint
            });

            AIAgent agent = client
                .GetChatClient(model?.ModelId)
                .CreateAIAgent()
                .AsBuilder()
                .Build();

            AgentRunResponse response = await agent.RunAsync("What is Kablam?");

            Console.WriteLine(response.Text);

            //ApiKeyCredential key = new ApiKeyCredential(manager.ApiKey);
            //OpenAIClient client = new OpenAIClient(key, new OpenAIClientOptions
            //{
            //    Endpoint = manager.Endpoint
            //});

            //var agent = client.GetChatClient(model?.ModelId).CreateAIAgent();

            //var completionUpdates = agent.RunAsync("What is Kablam?");

            //Console.Write($"[ASSISTANT]: ");
            //foreach (var completionUpdate in completionUpdates)
            //{
            //    if (completionUpdate.ContentUpdate.Count > 0)
            //    {
            //        Console.Write(completionUpdate.ContentUpdate[0].Text);
            //    }
            //}
            Console.ReadKey();
        }
    }
}
