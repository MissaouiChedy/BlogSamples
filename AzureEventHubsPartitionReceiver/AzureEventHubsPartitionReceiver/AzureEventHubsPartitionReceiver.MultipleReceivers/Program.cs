using Azure.Identity;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs;

string eventhubNamespace = "evh-test-partition-receiver.servicebus.windows.net";
string eventhubName = "main-topic";
string consumerGroup = "main-consumer";
string firstParition = "0";

/*
 * Create an event hubs partition receiver
 * receiving from latest event in the event stream
 */

var latestReceiver = new PartitionReceiver(
    consumerGroup,
    firstParition,
    EventPosition.Latest,
    eventhubNamespace,
    eventhubName,
    new DefaultAzureCredential(),
    new PartitionReceiverOptions()
    {
        Identifier = "LatestReceiver"
    }
);

/*
 * Create an event hubs partition receiver
 * receiving from range of events in the event stream
 */
var rangeReceiver = new PartitionReceiver(
    consumerGroup,
    firstParition,
    EventPosition.FromSequenceNumber(5), // starting position
    eventhubNamespace,
    eventhubName,
    new DefaultAzureCredential(),
    new PartitionReceiverOptions()
    {
        Identifier = "RangeReceiver"
    }
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

/*
 * Launch two receivers concurrently
 */

Task[] tasks = [
    Task.Factory.StartNew(() => ReceiveAsync(latestReceiver, cancellationSource).Wait()),
    Task.Factory.StartNew(() => ReceiveWithLimitAsync(
        rangeReceiver,
        cancellationSource,
        countLimit: 5)
    .Wait()),
];

Console.WriteLine("Starting event consumption, press Ctrl + C to halt...");

Task.WaitAll(tasks);

async Task ReceiveAsync(PartitionReceiver receiver, CancellationTokenSource cancellationSource)
{
    int eventsCounter = 0;
    try
    {
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
                eventsCounter++;
            }
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine($"{receiver.Identifier} Consumption Canceled With {eventsCounter} events!");
    }
    finally
    {
        await receiver.CloseAsync();
    }
}

async Task ReceiveWithLimitAsync(
    PartitionReceiver receiver, 
    CancellationTokenSource cancellationSource,
    int countLimit = 100)
{
    int eventCount = 0;
    try
    {
        while (!cancellationSource.IsCancellationRequested
            && (eventCount < countLimit))
        {
            int batchSize = 5;
            TimeSpan eventPullingSpan = TimeSpan.FromSeconds(1);

            IEnumerable<EventData> eventBatch = await receiver.ReceiveBatchAsync(
                batchSize,
                eventPullingSpan,
                cancellationSource.Token);

            foreach (EventData eventData in eventBatch)
            {
                eventCount++;
            }
        }

        Console.WriteLine($"{receiver.Identifier} Consumption Limit Reached With {eventCount} events!");
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine($"{receiver.Identifier} Consumption Canceled With {eventCount} events!");
    }
    finally
    {
        await receiver.CloseAsync();
    }
}

