using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace AgentFrameworkTest
{
    public static class PersistentAgentSample
    {
        public static async Task Run()
        {
            string endpoint = "https://aif-main-foundry-f1cc.services.ai.azure.com/api/projects/proj-main-f1cc";
            string deploymentName = "incident-classification-gpt41";

            PersistentAgentsClient client = new PersistentAgentsClient(endpoint, new DefaultAzureCredential());

            PersistentAgent? agentInfo = client.Administration
                .GetAgents()
                .FirstOrDefault(a => a.Name == "Agent003");


            if (agentInfo is null)
            {
                Response<PersistentAgent> aiFoundryAgent = await client.Administration.CreateAgentAsync(
                    deploymentName,
                    "Agent003",
                    "A question answerer",
                    "You are a helpful assistant that provides concise answers that do not exceed 50 words."
                    );

                agentInfo = aiFoundryAgent.Value;
            }

            try
            {
                var agent = await client.GetAIAgentAsync(agentInfo.Id);

                var thread = agent.GetNewThread();

                bool shouldContinue = true;
                Random random = new Random();

                while (shouldContinue)
                {
                    Console.WriteLine("Enter your message (or press Enter to quit): ");
                    string userMessage = Console.ReadLine() ?? "";

                    if (string.IsNullOrEmpty(userMessage))
                    {
                        shouldContinue = false;
                    }
                    else
                    {
                        List<AgentRunResponseUpdate> updates = new();

                        await foreach (var update in agent.RunStreamingAsync(userMessage, thread))
                        {
                            updates.Add(update);
                            Console.Write(update);
                            await Task.Delay(random.Next(10, 100));

                        }
                        Console.WriteLine();

                        var response = updates.ToAgentRunResponse();

                        Console.WriteLine($"Input tokens: {response?.Usage?.InputTokenCount}");
                        Console.WriteLine($"Output Tokens: {response?.Usage?.OutputTokenCount}");
                        Console.WriteLine($"Total Tokens: {response?.Usage?.TotalTokenCount}");
                    }
                    Console.WriteLine($"==============================================");
                }
            }
            finally
            {
                await client.Administration.DeleteAgentAsync(agentInfo.Id);
            }
        }
        
    }

    public record Response(string Summary, string Content, long ContentLength);
}
