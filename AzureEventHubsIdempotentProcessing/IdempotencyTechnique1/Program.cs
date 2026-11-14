using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using IdempotencyTechnique1;
using StackExchange.Redis;

const string eventHubNamespace = "evh-test-main-eventhub-ns-67fd.servicebus.windows.net";
const string eventHub = "technique1-topic";
const string consumerGroup = "technique1-consumer";
const string checkpointContainerUri = "https://stevhidemcheckpoint67fd.blob.core.windows.net/technique1-checkpoint";
const string redisEndpoint ="amr-evh-idem-67fd.swedencentral.redis.azure.net:10000";

/*
 * The TTL must exceed the worst-case replay window. Consumers can't rewind
 * further than the event hub retention (1 day), so 25 hours covers any replay
 */
TimeSpan processedEventTtl = TimeSpan.FromHours(25);

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
 * Connect to Azure Managed Redis with Entra ID
 */
ConfigurationOptions redisOptions = await ConfigurationOptions
    .Parse($"{redisEndpoint},ssl=true")
    .ConfigureForAzureWithTokenCredentialAsync(credential);

redisOptions.AbortOnConnectFail = true;

using ConnectionMultiplexer redisConnection = await ConnectionMultiplexer
    .ConnectAsync(redisOptions);

ProcessedEventsStore processedEventsStore = new(
    redisConnection,
    eventHubNamespace.Replace(".servicebus.windows.net", ""),
    eventHub,
    consumerGroup,
    processedEventTtl);

FakeEmailSender emailSender = new();

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

EventConsumer eventConsumer = new(processor, processedEventsStore, emailSender, checkpointThreshold);

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
 * of their partition are replayed on the next run, and the processed events store
 * skips them (with checkpointThreshold = 1 every event is checkpointed, so nothing is replayed)
 */

await eventConsumer.StopConsumptionAsync();
await eventSender.DisposeAsync();
