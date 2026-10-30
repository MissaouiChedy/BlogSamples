
using Azure.Identity;
using OpenAI.Responses;
using System.ClientModel.Primitives;
using System.Diagnostics;

namespace AgentFrameworkTest;

public static class OpenAIAPISample
{
    public static async Task Run()
    {
        string openAIEndpoint = "https://aif-main-foundry-a8a2.services.ai.azure.com/openai/v1/";
        
        string deploymentName = "gpt-5.6-terra";

#pragma warning disable OPENAI001
        ResponsesClient client = new(
            new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
            new ResponsesClientOptions { Endpoint = new Uri(openAIEndpoint) });

        CreateResponseOptions options = new()
        {
            Model = deploymentName,

        };
        options.InputItems.Add(ResponseItem.CreateSystemMessageItem(
            "You are a friendly assistant."));
        options.InputItems.Add(ResponseItem.CreateUserMessageItem("Provide a Quicksort implementation in Haskell"));


        var watch = Stopwatch.StartNew();
        ResponseResult response = await client.CreateResponseAsync(options);
        watch.Stop();
        Console.WriteLine(response.GetOutputText());
        Console.WriteLine($"Response time: {watch.Elapsed.Seconds}s");
#pragma warning restore OPENAI001
    }
}
