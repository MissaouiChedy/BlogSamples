using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using System.Text;
using System.Text.Json;

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
    "evh-test-consuming.servicebus.windows.net",
    "main-topic",
    new DefaultAzureCredential());

/*
 * Register event handling methods for processing messages and errors
 */
processor.ProcessEventAsync += ProcessEventHandler;
processor.ProcessErrorAsync += ProcessErrorHandler;

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
 * Define the event processing method
 */
async Task ProcessEventHandler(ProcessEventArgs eventArgs)
{
    try
    {
        /*
         * Effeciently Deserialize the Message from JSON
         */
        var readOnlySpan = new ReadOnlySpan<byte>(eventArgs.Data.Body.ToArray());
        Message receivedMessage = JsonSerializer
            .Deserialize<Message>(readOnlySpan);

        if (receivedMessage.isValid())
        {
            Console.WriteLine($"\tReceived message: {receivedMessage}");
        }
        else
        {
            string unknownMessage = Encoding
                .UTF8
                .GetString(eventArgs.Data.Body.ToArray());

            Console.WriteLine($"\tReceived Unknown Message Format: {unknownMessage}");
        }
    }
    catch (JsonException)
    {
        /*
         * JSON Deserialization errors are handled
         * in the catch block
         */
        string unknownMessage = Encoding
                .UTF8
                .GetString(eventArgs.Data.Body.ToArray());

        Console.WriteLine($"\tReceived Non Parsable Message: {unknownMessage}");
    }
    /*
     * Update the checkpoint store to mark the event as processed
     */
    await eventArgs.UpdateCheckpointAsync();
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
readonly record struct Message(Guid Id, DateTime TimeStamp, string Content)
{
    public bool isValid()
    {
        Message defaultMessage = default;

        return Id != defaultMessage.Id
            && TimeStamp != defaultMessage.TimeStamp
            && !string.IsNullOrEmpty(Content);
    }
}