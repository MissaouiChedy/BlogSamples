using Azure.Identity;
using StackExchange.Redis;
using AzureEventHubsEventProcessor;
using AzureEventHubsEventProcessor.CheckpointOwnershipStore;

string eventHubNameSpace = "evh-test-event-processor.servicebus.windows.net";
string eventHub = "main-topic";
string consumerGroup = "main-consumer";

StringWriter redisConnectionLog = new ();

int consumersCount = 3;

ConfigurationOptions configurationOptions = ConfigurationOptions
    .Parse($"127.0.0.1:6379");

configurationOptions.AbortOnConnectFail = true;

var consumers = Enumerable
    .Range(0, consumersCount)
    .Select((i) =>
    {
        var connectionMultiplexer = ConnectionMultiplexer
            .Connect(configurationOptions, redisConnectionLog);

        var store = new RedisCheckpointOwnershipStore(
                connectionMultiplexer,
                eventHubNameSpace.Replace(".servicebus.windows.net", ""),
                eventHub,
                consumerGroup);

        var processor = new CustomEventsProcessor(
            5,
            consumerGroup,
            eventHubNameSpace,
            eventHub,
            new DefaultAzureCredential(),
            store
        );

        processor.StartProcessing();

        return processor;
    })
    .ToList();

Console.WriteLine($"All '{consumersCount}' Consumers started, waiting for console input to stop...");
Console.ReadKey();
Console.WriteLine("");
Console.WriteLine("=================================================");

List<EventPayload> processedEvents = [];
foreach (var c in consumers)
{
    await c.StopProcessingAsync();
    Console.WriteLine($"Consumer {c.Identifier} Received '{c.ProcessedEvents.Count}' Events");
}
