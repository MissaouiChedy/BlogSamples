using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text;
using System.Text.Json;

int eventsCount = 10;

EventHubProducerClient evhProducer = new EventHubProducerClient(
    "evh-test-sending.servicebus.windows.net",
    "main-topic",
    new DefaultAzureCredential());

using EventDataBatch eventBatch = await evhProducer.CreateBatchAsync();

Message message = new(
    Guid.NewGuid(),
    DateTime.UtcNow,
    "KABLAM"
);

// Create a Batch of Messages
Enumerable
    .Range(0, eventsCount)
    .ToList()
    .ForEach((_) =>
    {
        if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)))))
        {
            throw new Exception($"Event is too large for the batch and cannot be sent.");
        }
    });

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

readonly record struct Message(Guid Id, DateTime TimeStamp, string Content) { }