using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using IdempotencyTechnique3;

const string eventHubNamespace = "evh-test-main-eventhub-ns-67fd.servicebus.windows.net";
const string eventHub = "technique3-topic";
const string consumerGroup = "technique3-consumer";
const string checkpointContainerUri = "https://stevhidemcheckpoint67fd.blob.core.windows.net/technique3-checkpoint";
const string inventoryTableEndpoint = "https://stevhidemstate67fd.table.core.windows.net";
const string inventoryTable = "inventory";

/*
 * Checkpoint every n events per partition
 */
const int checkpointThreshold = 2;

string[] skus = [
    "SKU-KEYBOARD",
    "SKU-MOUSE",
    "SKU-MONITOR",
    "SKU-HEADSET"
];

DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions
{
    ExcludeManagedIdentityCredential = true
});

/*
 * Connect to the inventory table with Entra ID, the table is the natural state of the consumer
 */
TableClient inventoryTableClient = new(
    new Uri(inventoryTableEndpoint),
    inventoryTable,
    credential);

ProductStockRepository productStockRepository = new(inventoryTableClient);

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

EventSender eventSender = new(evhProducer, skus);

EventConsumer eventConsumer = new(processor, productStockRepository, checkpointThreshold);

/*
 * Start event processing in the background
 */

await eventConsumer.StartConsumptionAsync();

Console.WriteLine("Processing events, press 's' to send a random stock adjustment, 'r' to resend an already sent adjustment, 'o' to send two adjustments out of order, 'q' to quit...");

/*
 * Listen for key presses while the processor consumes events concurrently
 */

while (true)
{
    ConsoleKey key = Console.ReadKey(intercept: true).Key;

    if (key == ConsoleKey.S)
    {
        await eventSender.SendRandomAdjustmentAsync();
    }
    else if (key == ConsoleKey.R)
    {
        await eventSender.ResendRandomAdjustmentAsync();
    }
    else if (key == ConsoleKey.O)
    {
        await eventSender.SendOutOfOrderAdjustmentsAsync();
    }
    else if (key == ConsoleKey.Q)
    {
        break;
    }
}

/*
 * Stop gracefully without a final checkpoint: events processed since the last checkpoint
 * of their partition are replayed on the next run, and their Lsn is found already applied
 * (with checkpointThreshold = 1 every event is checkpointed, so nothing is replayed)
 */

await eventConsumer.StopConsumptionAsync();
await eventSender.DisposeAsync();
