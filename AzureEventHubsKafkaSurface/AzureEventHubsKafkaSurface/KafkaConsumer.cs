using Confluent.Kafka;

namespace AzureEventHubsKafkaSurface;

/*
 * KafkaConsumer is responsible for consuming events from the configured Topic and checkpointing offsets.
 * It also supports rewinding the stream to the beginning on demand.
 */
public class KafkaConsumer
{
    private readonly string _bootstrapServers;
    private readonly string _topicName;
    private readonly string _consumerGroup;
    private readonly Action<IClient, string> _oauthRefreshHandler;

    private volatile bool _rewindRequested;

    public KafkaConsumer(
        string bootstrapServers,
        string topicName,
        string consumerGroup,
        Action<IClient, string> oauthRefreshHandler)
    {
        _bootstrapServers    = bootstrapServers;
        _topicName           = topicName;
        _consumerGroup       = consumerGroup;
        _oauthRefreshHandler = oauthRefreshHandler;
    }

    /*
     * Signal that a rewind to the beginning of the topic should be performed before the next Consume call.
     */
    public void RequestRewind() => _rewindRequested = true;

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism    = SaslMechanism.OAuthBearer,
            GroupId          = _consumerGroup,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Explicit offset management i.e. checkpointing
        };

        /*
         * CancellationToken.None is used here so the task is always scheduled; cancellation is
         * handled inside the loop so that consumer.Close() always runs.
         */
        return Task.Run(() =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetOAuthBearerTokenRefreshHandler(_oauthRefreshHandler)
                .Build();

            consumer.Subscribe(_topicName);
            Console.WriteLine("[Consumer] Subscribed. Polling for messages...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    /*
                     * Handle a pending rewind before the next Consume call.
                     */
                    HandleStreamRewind(consumer);

                    /*
                     * Poll for available events with a relatively short timeout.
                     */
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(800));
                    if (result is null)
                        continue;

                    Console.WriteLine(
                        $"[Consumer] Key={result.Message.Key} | " +
                        $"Value={result.Message.Value} | " +
                        $"Partition={result.Partition} | Offset={result.Offset}");

                    /*
                     * Checkpoint the offset of the event processed.
                     */
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"[Consumer] Consume error: {ex.Error.Reason}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            consumer.Close();
            Console.WriteLine("[Consumer] Stopped.");
        }, CancellationToken.None);
    }

    /*
     * If a rewind is requested, seek all assigned partitions back to the beginning.
     */
    private void HandleStreamRewind(IConsumer<string, string> consumer)
    {
        if (!_rewindRequested) return;

        _rewindRequested = false;
        
        List<TopicPartition> assignedPartitions = consumer.Assignment;

        if (assignedPartitions.Count > 0)
        {
            foreach (var topicPartition in assignedPartitions)
            {
                /*
                 * Perform a Seek to the beginning of each assigned partition.
                 */
                consumer.Seek(new TopicPartitionOffset(topicPartition.Topic, topicPartition.Partition, Offset.Beginning));
            }

            Console.WriteLine($"[Consumer] Rewound {assignedPartitions.Count} partition(s) to the beginning.");
        }
        else
        {
            Console.WriteLine("[Consumer] No partitions assigned yet: rewind skipped.");
        }
    }
}
