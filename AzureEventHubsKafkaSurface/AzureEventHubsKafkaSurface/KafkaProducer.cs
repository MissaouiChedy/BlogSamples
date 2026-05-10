using Confluent.Kafka;

namespace AzureEventHubsKafkaSurface;

/*
 * KafkaProducer is responsible for sending events at specified interval
 * to the configured Topic.
 */
public class KafkaProducer
{
    private readonly string _bootstrapServers;
    private readonly string _topicName;
    private readonly Action<IClient, string> _oauthRefreshHandler;
    private readonly TimeSpan _sendInterval;

    public KafkaProducer(
        string bootstrapServers,
        string topicName,
        Action<IClient, string> oauthRefreshHandler,
        TimeSpan? sendInterval = null)
    {
        _bootstrapServers    = bootstrapServers;
        _topicName           = topicName;
        _oauthRefreshHandler = oauthRefreshHandler;
        _sendInterval        = sendInterval ?? TimeSpan.FromSeconds(3);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        /*
         * Create Kafka Producer configuration with SASL OAUTHBEARER settings.
         */
        var config = new ProducerConfig
        {
            BootstrapServers      = _bootstrapServers,
            SecurityProtocol      = SecurityProtocol.SaslSsl,
            SaslMechanism         = SaslMechanism.OAuthBearer,
            SaslOauthbearerMethod = SaslOauthbearerMethod.Default,
        };

        using var producer = new ProducerBuilder<string, string>(config)
            .SetOAuthBearerTokenRefreshHandler(_oauthRefreshHandler)
            .Build();

        Console.WriteLine("[Producer] Started.");

        /*
         * While Cancellation not requested, produce messages at regular interval.
         */
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key   = Guid.NewGuid().ToString(), // Partition Key
                    Value = $"Message from Kafka over Event Hubs at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}",
                };

                var delivery = await producer.ProduceAsync(_topicName, message, cancellationToken);
                Console.WriteLine($"[Producer] Delivered Message to partition {delivery.Partition.Value}, offset {delivery.Offset}");

                await Task.Delay(_sendInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Producer] Error: {ex.Message}");
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        Console.WriteLine("[Producer] Stopped.");
    }
}
