using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

int eventsCount = 10;
/*
 * Initialize the Producer Client Object
 */
EventHubProducerClient evhProducer = new EventHubProducerClient(
    "evh-test-sending.servicebus.windows.net",
    "main-topic",
    new DefaultAzureCredential());

/*
 * Use the Producer Client to create an event batch
 */
using EventDataBatch eventBatch = await evhProducer.CreateBatchAsync();

/*
 * Create a Batch of Messages
 */
Enumerable
    .Range(0, eventsCount)
    .ToList()
    .ForEach((_) =>
    {
        Message message = new(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "KABLAM"
        );

        byte[] messageInBinary = Encoding
            .UTF8
            .GetBytes(JsonSerializer.Serialize(message));

        if (!eventBatch.TryAdd(new EventData(messageInBinary)))
        {
            throw new Exception($"Event is too large for the batch and cannot be sent.");
        }
    });

/*
 * Send The Event Batch, read key from console,
 * then dispose of Producer Client
 */
try
{
    await evhProducer.SendAsync(eventBatch);
    Console.WriteLine($"Batch of {eventsCount} events sent.");
    Console.ReadKey();
}
finally
{
    await evhProducer.DisposeAsync();
}

/*
 * Message Record Definition
 */
readonly record struct Message(Guid Id, DateTime TimeStamp, string Content) { }