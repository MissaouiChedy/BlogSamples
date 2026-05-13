using Azure.Core;
using Azure.Identity;
using AzureEventHubsKafkaSurface;
using Confluent.Kafka;


/*
 * Define connection parameters.
 */
string BootstrapServers = "evh-test-kafka-surface.servicebus.windows.net:9093";
string TopicName        = "main-topic";
string ConsumerGroup    = "main-consumer";
string EventHubsScope   = $"https://evh-test-kafka-surface.servicebus.windows.net/.default";

var credential = new DefaultAzureCredential();

/*
 * Define the SASL Oauth refresh handler.
 */
Action<IClient, string> oauthRefreshHandler = (client, _) =>
{
    try
    {
        var tokenRequestContext = new TokenRequestContext([EventHubsScope]);
        var accessToken = credential.GetToken(tokenRequestContext);
        
        client.OAuthBearerSetToken(
            tokenValue:    accessToken.Token,
            lifetimeMs:    accessToken.ExpiresOn.ToUnixTimeMilliseconds(),
            principalName: string.Empty,
            extensions:    new Dictionary<string, string>());
    }
    catch (Exception ex)
    {
        client.OAuthBearerSetTokenFailure(ex.ToString());
    }
};

/*
 * Register Ctrl+C handler.
 */
using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[Main] Ctrl+C pressed. Exiting gracefully...");
    cancellationTokenSource.Cancel();
};

/*
 * Create and run producer and consumer tasks.
 */
var producer = new KafkaProducer(BootstrapServers, TopicName, oauthRefreshHandler);
var consumer = new KafkaConsumer(BootstrapServers, TopicName, ConsumerGroup, oauthRefreshHandler);

var producerTask = producer.RunAsync(cancellationTokenSource.Token);
var consumerTask = consumer.RunAsync(cancellationTokenSource.Token);

Console.WriteLine("[Main] Running... Press Ctrl+C to stop, 'r' to rewind the consumer.");

/*
 * Listen for user input to trigger consumer rewind or graceful shutdown.
 */
while (!cancellationTokenSource.IsCancellationRequested)
{
    if (!Console.KeyAvailable)
    {
        try   { await Task.Delay(100, cancellationTokenSource.Token); }
        catch (OperationCanceledException) { break; }
        continue;
    }

    var key = Console.ReadKey(intercept: true);
    if (char.ToUpperInvariant(key.KeyChar) == 'R')
    {
        Console.WriteLine("\n[Main] Rewind requested.");
        consumer.RequestRewind();
    }
}

/*
 * Wait for tasks to complete and exit.
 */
await Task.WhenAll(producerTask, consumerTask);
Console.WriteLine("[Main] All tasks completed. Shutdown [OK]");
