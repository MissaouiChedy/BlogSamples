using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using AzureEventHubsPartitionReceiver;
using System.Text.Json;

string eventhubNamespace = "evh-test-partition-receiver.servicebus.windows.net";
string eventhubName = "main-topic";
string consumerGroup = "main-consumer";
string firstParition = "0";

/*
 * Create an event hubs parition receiver
 */
var receiver = new PartitionReceiver(
    consumerGroup,
    firstParition,
    EventPosition.Earliest,
    eventhubNamespace,
    eventhubName,
    new DefaultAzureCredential()
);

/*
 * Define Cancellation Behavior to halt the program via Ctrl +C
 */
using CancellationTokenSource cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    cancellationSource.Cancel();
    Console.WriteLine("\nCtrl+C pressed. Exiting gracefully...");
    e.Cancel = true; // Prevents immediate app termination
};

int eventsCount = 0 ;
Console.WriteLine("Starting event consumption, press Ctrl + C to halt...");
try
{
    /*
     * Read events from the partition
     */
    while (!cancellationSource.IsCancellationRequested)
    {
        int batchSize = 10;
        TimeSpan eventPullingSpan = TimeSpan.FromSeconds(1);

        IEnumerable<EventData> eventBatch = await receiver.ReceiveBatchAsync(
            batchSize,
            eventPullingSpan,
            cancellationSource.Token);

        foreach (EventData eventData in eventBatch)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(eventData.EventBody.ToArray());
            EventPayload receivedEvent = JsonSerializer
                .Deserialize<EventPayload>(readOnlySpan)!;

            Console.WriteLine($"Received Seqnum: {eventData.SequenceNumber}, event: '{receivedEvent}'");

            eventsCount++;
        }
        Console.WriteLine("==================================");
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("Consumption Canceled !");
}
finally
{
    await receiver.CloseAsync();
    Console.WriteLine($"Terminated with {eventsCount} events");
}

