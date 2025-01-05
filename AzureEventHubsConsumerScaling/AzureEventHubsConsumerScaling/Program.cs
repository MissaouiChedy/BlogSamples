using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs;
using AzureEventHubsConsumerScaling;
using System.Collections.Concurrent;

/*
 * Consumer Count is set in the consumersCount variable 
 */
int consumersCount = 8;

/*
 * Create Consumers dynamically using the EventConsumer Class
 */
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

/*
 * Show consumers count created and wait for user input to terminate consumption
 */
Console.WriteLine($"All '{consumersCount}' Consumers started, waiting for console input to stop...");
Console.ReadKey();
Console.WriteLine("");
Console.WriteLine("=================================================");

/*
 * Stop consumers and collect message received with metadata (partition id and Timestamps)
 */
ConcurrentBag<MessageReceived> receivedMessages = new ConcurrentBag<MessageReceived>();
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

/*
 * Group messages by Parition Id
 */
Dictionary<string, List<MessageReceived>> receivedMessagesByPartitions = receivedMessages
    .GroupBy(r => r.PartitionId)
    .ToDictionary(g => g.Key, g => g.ToList());

foreach (string key in receivedMessagesByPartitions.Keys)
{
    var locationIdsForParition = string.Join(",", receivedMessagesByPartitions[key]
        .Select(m => m.Message.LocationId)
        .OrderBy(id => id)
        .Distinct());

    Console.WriteLine($"Location Ids received on Partition {key} '{locationIdsForParition}'");
}

/*
 * Determine duplicate messages across paritions
 */
List<DuplicateMessage> duplicateMessages = GetDuplicateMessages(receivedMessagesByPartitions);

Console.WriteLine("============================================================================");
Console.WriteLine($"{duplicateMessages.Count} Duplicate Messages found with the following Ids:");
foreach (var id in duplicateMessages)
{
    Console.WriteLine($"Message Id: {id.MessageId}, duplicated {id.DuplicateCount - 1} time(s)");
}

Console.WriteLine("============================================================================");

/*
 * Determine Last Received Duplicate Message TimeStamp and Last Received Message TimeStamp
 */
if (duplicateMessages.Count > 0)
{
    var lastDuplicatedMessageTimeStamp = duplicateMessages
        .SelectMany(m => m.TimeStamps)
        .Max();

    DateTime lastReceivedMessageTimeStamp = receivedMessagesByPartitions
        .SelectMany(kvp => kvp.Value)
        .Select(m => m.Timestamp)
        .Max();

    Console.WriteLine($"Last Duplicate Message was Received At {lastDuplicatedMessageTimeStamp}");
    Console.WriteLine($"Last Message Received At {lastReceivedMessageTimeStamp}");
}
else
{
    Console.WriteLine($"No Duplicate Message Received");
}

static List<DuplicateMessage> GetDuplicateMessages(
    Dictionary<string, List<MessageReceived>> receivedMessagesByPartitions)
{
    return receivedMessagesByPartitions
        .SelectMany(kvp => kvp.Value)
        .GroupBy(m => m.Message.Id)
        .Where(g => g.Count() > 1)
        .Select(g => new DuplicateMessage(g.Key, g.Count(), g.Select(r => r.Timestamp).ToList()))
        .ToList();
}
record DuplicateMessage(Guid MessageId, int DuplicateCount, List<DateTime> TimeStamps);
