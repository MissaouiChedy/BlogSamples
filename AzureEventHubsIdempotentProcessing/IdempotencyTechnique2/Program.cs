using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using IdempotencyTechnique2;

const string eventHubNamespace = "evh-test-main-eventhub-ns-67fd.servicebus.windows.net";
const string eventHub = "technique2-topic";
const string consumerGroup = "technique2-consumer";
const string checkpointContainerUri = "https://stevhidemcheckpoint67fd.blob.core.windows.net/technique2-checkpoint";
const string ordersTableEndpoint = "https://stevhidemstate67fd.table.core.windows.net";
const string ordersTable = "orders";

/*
 * Checkpoint every n events per partition
 */
const int checkpointThreshold = 2;

string[] customerEmails = [
    "alice@techdominator.com",
    "bob@techdominator.com",
    "carol@techdominator.com",
    "dave@techdominator.com"
];

DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions
{
    ExcludeManagedIdentityCredential = true
});

/*
 * Connect to the orders table with Entra ID, the table is the natural state of the consumer
 */
TableClient ordersTableClient = new(
    new Uri(ordersTableEndpoint),
    ordersTable,
    credential);

OrderRepository orderRepository = new(ordersTableClient);

EventHubProducerClient evhProducer = new(
    eventHubNamespace,
    eventHub,
    credential);

BlobContainerClient storageClient = new(
    new Uri(checkpointContainerUri),
    credential);

EventProcessorClient processor = new(
    storageClient,
    consumerGroup,
    eventHubNamespace,
    eventHub,
    credential);

EventSender eventSender = new(evhProducer, customerEmails);

EventConsumer eventConsumer = new(processor, orderRepository, checkpointThreshold);

/*
 * Start event processing in the background
 */

await eventConsumer.StartConsumptionAsync();

Console.WriteLine("Processing events, press 's' to send a random order, 'r' to resend an already sent order, 'q' to quit...");

/*
 * Listen for key presses while the processor consumes events concurrently
 */

while (true)
{
    ConsoleKey key = Console.ReadKey(intercept: true).Key;

    if (key == ConsoleKey.S)
    {
        await eventSender.SendRandomOrderAsync();
    }
    else if (key == ConsoleKey.R)
    {
        await eventSender.ResendRandomOrderAsync();
    }
    else if (key == ConsoleKey.Q)
    {
        break;
    }
}

/*
 * Stop gracefully without a final checkpoint: events processed since the last checkpoint
 * of their partition are replayed on the next run, and the orders table rejects them
 * (with checkpointThreshold = 1 every event is checkpointed, so nothing is replayed)
 */

await eventConsumer.StopConsumptionAsync();
await eventSender.DisposeAsync();
