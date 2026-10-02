using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;

namespace AgentFrameworkTest;

public static class BasicAgentSample
{
    public static async Task Run()
    {
        string endpoint = "https://aif-main-foundry-a8a2.services.ai.azure.com/api/projects/proj-main-a8a2";
        string deploymentName = "gpt-5.6-terra";

        AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
            .AsAIAgent(
                model: deploymentName,
                instructions: "You are a friendly assistant. Keep your answers brief.",
                name: "Agent010");

        var response = await agent.RunAsync("What is the capital of France ?");

        Console.WriteLine(response.Text);
    }
}
