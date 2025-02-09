using Azure.Identity;
using System.Diagnostics;
using StackExchange.Redis;
using Azure.Messaging.EventHubs.Consumer;
using AzureEventhubsConsumerClient.CheckpointStore;

/*
 * Create a redis connection multiplexer
 */
var redisConnectionLog = new StringWriter();

//var configurationOptions = ConfigurationOptions
//    .Parse($"127.0.0.1:6379");

var configurationOptions = ConfigurationOptions
    .Parse($"rds-test-consumer-client.redis.cache.windows.net:6380");

await configurationOptions
    .ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());

configurationOptions.AbortOnConnectFail = true;

using var connectionMultiplexer = await ConnectionMultiplexer
    .ConnectAsync(configurationOptions, redisConnectionLog);

/*
 * Create an event hubs consumer client
 */
var consumer = new EventHubConsumerClient(
    "main-consumer",
    "evh-test-consumer-client.servicebus.windows.net",
    "main-topic",
    new DefaultAzureCredential());

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

try
{
    /*
     * Determine partition and the starting position for reading events
     */
    string firstPartition = (await consumer.GetPartitionIdsAsync(cancellationSource.Token)).First();

    AbstractCheckpointStore checkpointStore = new RedisCheckpointStore(
        consumer.EventHubName,
        consumer.ConsumerGroup,
        firstPartition,
        connectionMultiplexer);

    long latestSequenceNumber = await checkpointStore.GetSequenceNumberAsync();
    
    EventPosition startingPosition = EventPosition.Earliest;
    if (latestSequenceNumber > 0)
    {
        startingPosition = EventPosition.FromSequenceNumber(latestSequenceNumber);
    }

    /*
     * Read events from the partition
     */
    Console.WriteLine("Starting event consumption, press Ctrl + C to halt...");
    await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(
        firstPartition,
        startingPosition,
        cancellationSource.Token))
    {
        string readFromPartition = partitionEvent.Partition.PartitionId;
        ReadOnlyMemory<byte> eventBodyBytes = partitionEvent.Data.EventBody.ToMemory();

        Console.WriteLine($"{partitionEvent.Data.SequenceNumber}: Received event of length {eventBodyBytes.Length} from partition: {readFromPartition}");

        /*
         * Checkpoint with duration measurement
         */
        Stopwatch stopwatch = Stopwatch.StartNew();
        await checkpointStore.SetSequenceNumberAsync(partitionEvent.Data.SequenceNumber);
        stopwatch.Stop();
        Console.WriteLine($" => Checkpointing took {stopwatch.ElapsedMilliseconds} ms");
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("Consumption Canceled !");
}
finally
{
    await consumer.CloseAsync();
    Console.WriteLine(redisConnectionLog);
    Console.WriteLine("Terminated !");
}
