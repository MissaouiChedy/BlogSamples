using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using System.Diagnostics;
using System.Text.Json;

int checkpointCounter = 0;
int checkpointThreshold = 5;
List<long> offsets = new();

/*
 * Create a Blob Container Client Used For check-pointing
 */
var storageClient = new BlobContainerClient(
    new Uri("https://mainconsumerstorageacc.blob.core.windows.net/main-consumer"),
    new DefaultAzureCredential());

/*
 * Create an Event Processor Client for Consuming Events
 */
var processor = new EventProcessorClient(
    storageClient,
    "main-consumer",
    "evh-test-checkpoint-rewind.servicebus.windows.net",
    "main-topic",
    new DefaultAzureCredential()
    );

/*
 * Register event handling methods for initialization, processing messages and errors
 */
processor.ProcessEventAsync += ProcessEventHandler;
processor.ProcessErrorAsync += ProcessErrorHandler;

/*
 * Register Initialization Event Handler Containing Event Stream Rewind
 * Set one of:
 *   - InitializePartitionWithOffsetRewind
 *   - InitializePartitionWithSequenceRewind
 *   - InitializePartitionWithTimestampRewind
 */

//processor.PartitionInitializingAsync += InitializePartitionWithSequenceRewind;

/*
 * Start event processing
 */
await processor.StartProcessingAsync();

Console.WriteLine("Waiting for key press to stop processing...");
Console.ReadKey();

/*
 * Stop event processing before exiting the application
 */
await processor.StopProcessingAsync();

/*
 * Compute and Show Diffs Between Consecutive Offsets
 */
var offsetDiffs = offsets
    .Skip(1)
    .Select((x, i) => x - offsets[i]);

Console.WriteLine($"");
Console.WriteLine($"Offset diffs: {string.Join(", ", offsetDiffs)}");

/*
 * Define the event processing method
 */
async Task ProcessEventHandler(ProcessEventArgs eventArgs)
{
    var readOnlySpan = new ReadOnlySpan<byte>(eventArgs.Data.Body.ToArray());
    Message? receivedMessage = JsonSerializer
        .Deserialize<Message>(readOnlySpan);
    offsets.Add(eventArgs.Data.Offset);
    Console.WriteLine($"Received message: {receivedMessage}");
    Console.WriteLine($"\tWith (Offset: {eventArgs.Data.Offset}, SeqNum: {eventArgs.Data.SequenceNumber})");

    /*
     * Update the checkpoint store to mark the event as processed
     */

    checkpointCounter += 1;

    if (checkpointCounter >= checkpointThreshold)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await eventArgs.UpdateCheckpointAsync();
        stopwatch.Stop();
        Console.WriteLine($" ==> Checkpointed, took {stopwatch.ElapsedMilliseconds} ms");
        checkpointCounter = 0;
    }
}

async Task InitializePartitionWithOffsetRewind(PartitionInitializingEventArgs initArgs)
{
    Console.WriteLine("Initializing Partition with Offset Rewind...");
    var blobClient = storageClient
        .GetBlobClient($"evh-test-checkpoint-rewind.servicebus.windows.net/main-topic/main-consumer/checkpoint/{initArgs.PartitionId}");

    if (blobClient.Exists())
    {
        var blobProps = await blobClient.GetPropertiesAsync();
        long offset = Convert.ToInt64(blobProps.Value.Metadata["offset"]);
        /*
         * Rewind the offset by 1000 bytes
         */
        long rewindedOffset = offset - 1000;

        await blobClient.DeleteAsync();

        initArgs.DefaultStartingPosition = EventPosition
            .FromOffset(rewindedOffset);
    }
    else
    {
        Console.WriteLine("Checkpoint Blob does not exist");
    }
}

async Task InitializePartitionWithSequenceRewind(PartitionInitializingEventArgs initArgs)
{
    Console.WriteLine("Initializing Partition with Sequence Rewind...");

    var blobClient = storageClient
        .GetBlobClient($"evh-test-checkpoint-rewind.servicebus.windows.net/main-topic/main-consumer/checkpoint/{initArgs.PartitionId}");

    if (blobClient.Exists())
    {
        var blobProps = await blobClient.GetPropertiesAsync();
        long sequenceNumber = Convert.ToInt64(blobProps.Value.Metadata["sequenceNumber"]);
        /*
         * Rewind the sequenceNumber by 10 events
         */
        long rewindedSequenceNumber = sequenceNumber - 9;
        
        await blobClient.DeleteAsync();

        initArgs.DefaultStartingPosition = EventPosition
            .FromSequenceNumber(rewindedSequenceNumber);
    }
    else
    {
        Console.WriteLine("Checkpoint Blob does not exist");
    }
}

async Task InitializePartitionWithTimestampRewind(PartitionInitializingEventArgs initArgs)
{
    Console.WriteLine("Initializing Partition with TimeStamp Rewind...");

    var blobClient = storageClient
        .GetBlobClient($"evh-test-checkpoint-rewind.servicebus.windows.net/main-topic/main-consumer/checkpoint/{initArgs.PartitionId}");

    if (blobClient.Exists())
    {
        await blobClient.DeleteAsync();
        
        /*
         * Rewind the event stream to 10 minutes ago
         */
        DateTimeOffset rewindTime = DateTimeOffset
            .UtcNow
            .AddMinutes(-10);

        initArgs.DefaultStartingPosition = EventPosition
            .FromEnqueuedTime(rewindTime);
    }
    else
    {
        Console.WriteLine("Checkpoint Blob does not exist");
    }
}

/*
 * Define the Error Handler Method
 */
Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
{
    Console.WriteLine(@$"\tPartition '{eventArgs.PartitionId}':
    an unhandled exception was encountered. This was not expected to happen.");
    Console.WriteLine(eventArgs.Exception.Message);
    return Task.CompletedTask;
}

/*
 * Message Record Definition
 */
public record Message(Guid Id, string LocationId, string Content);