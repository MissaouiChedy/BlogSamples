/*
 * https://blog.pnop.co.jp/jmeter-azure-event-hubs_en/jj
 */
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using AzureEventHubsConsumerScaling;
using System.Collections.Concurrent;

int consumersCount = 3;
ConcurrentBag<MessageReceived> receivedMessages = new ConcurrentBag<MessageReceived>();

var consumers = Enumerable
    .Range(0, consumersCount)
    .Select((i) =>
    {
        var storageClient = new BlobContainerClient(
            new Uri("https://mainconsumerstorageacc.blob.core.windows.net/main-consumer"),
            new DefaultAzureCredential());

        var processor = new EventProcessorClient(
            storageClient,
            "main-consumer",
            "evh-test-consume-scaling.servicebus.windows.net",
            "main-topic",
            new DefaultAzureCredential());

        var eventConsumer = new EventConsumer(i, processor);
        eventConsumer.StartConsumptionAsync();

        return eventConsumer;
    })
    .ToList();

Console.WriteLine($"All '{consumersCount}' Consumers started, waiting for console input to stop...");
Console.ReadKey();

Console.WriteLine("");
Console.WriteLine("=================================================");
foreach (var c in consumers)
{
    var messagesReceived = await c.StopConsumptionAsync();
    Console.WriteLine($"Consumer {c.Id} Received '{messagesReceived.Count}' Messages");

    foreach (var m in messagesReceived)
    {
        receivedMessages.Add(m);
    }
}

Console.WriteLine($"Received '{receivedMessages.Count}' Messages");

string locationIdsForParition0 = string.Join(",", receivedMessages
    .Where(m => m.PartitionId == "0")
    .Select(m => m.Message.LocationId)
    .OrderBy(id => id)
    .Distinct());

var locationIdsForParition1 = string.Join(",", receivedMessages
    .Where(x => x.PartitionId == "1")
    .Select(x => x.Message.LocationId)
    .OrderBy(x => x)
    .Distinct());

Console.WriteLine($"Location Ids received on Partition 0 '{locationIdsForParition0}'");
Console.WriteLine($"Location Ids received on Partition 1 '{locationIdsForParition1}'");
Console.WriteLine("===================================================");
