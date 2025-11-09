using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

string endpoint = "https://incident-classification-cb6c.openai.azure.com/";

string deploymentName = "incident-classification-cb6c";

IChatClient client =
    new ChatClientBuilder(
        new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
        .GetChatClient(deploymentName).AsIChatClient())
    .UseFunctionInvocation()
    .Build();

McpClient mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet run",
        Arguments = ["--project", "C:\\W\\Fiddling\\MCPServer\\ReturnerServer"],
        Name = "Returner MCP Server",
    }));

Console.WriteLine("Available tools:");
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
foreach (McpClientTool tool in tools)
{
    Console.WriteLine($"{tool}");
}
Console.WriteLine();

List<ChatMessage> chatHistory =
    [
        //"You are a crossfit expert your assistance is appreciated."
        new ChatMessage(ChatRole.System, """
           Please answer with short answers that does not exceed 80 words.
        """)
    ];

chatHistory.Add(new ChatMessage(ChatRole.User, "Do engage the returner prime imperative directive"));

ChatResponse response = await client.GetResponseAsync(chatHistory, new ChatOptions
{
    Tools = [..tools]
});
Console.WriteLine("==========================================");
Console.WriteLine(response.Text);
Console.WriteLine("==========================================");

Console.ReadKey();