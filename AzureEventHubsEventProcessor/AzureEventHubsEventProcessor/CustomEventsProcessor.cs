using Azure.Core;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using System.Collections.Concurrent;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Primitives;
using AzureEventHubsEventProcessor.CheckpointOwnershipStore;

namespace AzureEventHubsEventProcessor
{
    public class CustomEventsProcessor : EventProcessor<CustomPartitionContext>
    {
        private readonly ICheckpointOwnershipStore _checkpointOwnershipStore;

        private readonly ConcurrentBag<EventPayload> _processedEvents = [];

        public List<EventPayload> ProcessedEvents => [.. _processedEvents];

        public CustomEventsProcessor(int batchSize, string consumerGroup,
            string evhNamespace,
            string evhTopic,
            TokenCredential credentials,
            ICheckpointOwnershipStore store)
            : base(batchSize, consumerGroup, evhNamespace, evhTopic, credentials, new()
            {
                LoadBalancingUpdateInterval = TimeSpan.FromSeconds(30),
                PartitionOwnershipExpirationInterval = TimeSpan.FromSeconds(60),
                Identifier = $"Consumer-{Guid.NewGuid().ToString().Split('-')[1]}",
                LoadBalancingStrategy = LoadBalancingStrategy.Greedy
            })
        {
            _checkpointOwnershipStore = store;
        }

        protected override async Task<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
            IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
            CancellationToken cancellationToken)
        {
            List<EventProcessorPartitionOwnership> ownerships = [];
            
            foreach (var ownership in desiredOwnership)
            {
                string version = Guid.NewGuid().ToString();

                await _checkpointOwnershipStore
                    .SetOwnershipAsync(
                        ownership.PartitionId, 
                        new Ownership(ownership.PartitionId, Identifier,
                            ownership.LastModifiedTime, version)
                    );

                ownerships.Add(new EventProcessorPartitionOwnership
                {
                    ConsumerGroup = ownership.ConsumerGroup,
                    EventHubName = ownership.EventHubName,
                    FullyQualifiedNamespace = ownership.FullyQualifiedNamespace,
                    OwnerIdentifier = Identifier,
                    PartitionId = ownership.PartitionId,
                    LastModifiedTime = ownership.LastModifiedTime,
                    Version = version
                });
            }

            return ownerships;
        }

        protected override async Task<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
            CancellationToken cancellationToken)
        {
            var ownership = await _checkpointOwnershipStore.GetAllOwnershipsAsync();

            return ownership
                .Select(o => new EventProcessorPartitionOwnership
                {
                    FullyQualifiedNamespace = _checkpointOwnershipStore.EventhubNamespace,
                    ConsumerGroup = _checkpointOwnershipStore.ConsumerGroup,
                    EventHubName = _checkpointOwnershipStore.Eventhub,
                    OwnerIdentifier = o.OwnerId,
                    PartitionId = o.PartitionId,
                    LastModifiedTime = o.LastModifiedTime,
                    Version = o.Version
                });
        }

        protected override Task OnProcessingErrorAsync(Exception exception, CustomPartitionContext partition,
            string operationDescription, CancellationToken cancellationToken)
        {
            Console.WriteLine("Processing Error !");
            Console.WriteLine("==================");
            Console.WriteLine(exception.Message);

            return Task.CompletedTask;
        }

        protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events,
            CustomPartitionContext partition, CancellationToken cancellationToken)
        {
            foreach (var eventData in events)
            {
                var readOnlySpan = new ReadOnlySpan<byte>(eventData.EventBody.ToArray());
                EventPayload receivedEvent = JsonSerializer
                    .Deserialize<EventPayload>(readOnlySpan)!;

                Console.WriteLine($"Consumer {Identifier} received '{receivedEvent}' from partition {partition.PartitionId}");

                _processedEvents.Add(receivedEvent);
                await UpdateCheckpointAsync(partition.PartitionId, eventData.SequenceNumber,
                    eventData.Offset, cancellationToken);
            }
        }
        protected override async Task<EventProcessorCheckpoint> GetCheckpointAsync(string partitionId, 
            CancellationToken cancellationToken)
        {
            Checkpoint checkpoint = await _checkpointOwnershipStore
                .GetCheckpointAsync(partitionId);

            if (checkpoint != Checkpoint.Null)
            {
                return new EventProcessorCheckpoint
                {
                    StartingPosition = EventPosition.FromSequenceNumber(checkpoint.SequenceNumber),
                    PartitionId = partitionId,
                    ConsumerGroup = _checkpointOwnershipStore.ConsumerGroup,
                    EventHubName = _checkpointOwnershipStore.Eventhub,
                    FullyQualifiedNamespace = _checkpointOwnershipStore.EventhubNamespace,
                    ClientIdentifier = checkpoint.OwnerId
                };
            }

            return new EventProcessorCheckpoint
            {
                StartingPosition = EventPosition.Earliest,
                PartitionId = partitionId,
                ConsumerGroup = _checkpointOwnershipStore.ConsumerGroup,
                EventHubName = _checkpointOwnershipStore.Eventhub,
                FullyQualifiedNamespace = _checkpointOwnershipStore.EventhubNamespace,
                ClientIdentifier = checkpoint.OwnerId
            };
        }

        protected override Task UpdateCheckpointAsync(string partitionId, long sequenceNumber,
            long? offset, CancellationToken cancellationToken)
        {
            return _checkpointOwnershipStore.SetCheckpointAsync(partitionId, new Checkpoint
            (
                partitionId,
                offset.Value,
                sequenceNumber,
                Identifier
            ));
        }

        protected override async Task<IEnumerable<EventProcessorCheckpoint>> ListCheckpointsAsync(
            CancellationToken cancellationToken)
        {
            var checkpoints = await _checkpointOwnershipStore.GetAllCheckpointsAsync();

            return checkpoints
                .Select(c => new EventProcessorCheckpoint
                {
                    StartingPosition = EventPosition.FromSequenceNumber(c.SequenceNumber),
                    PartitionId = c.PartitionId,
                    ConsumerGroup = _checkpointOwnershipStore.ConsumerGroup,
                    EventHubName = _checkpointOwnershipStore.Eventhub,
                    FullyQualifiedNamespace = _checkpointOwnershipStore.EventhubNamespace,
                    ClientIdentifier = c.OwnerId
                });
        }
    }
}
