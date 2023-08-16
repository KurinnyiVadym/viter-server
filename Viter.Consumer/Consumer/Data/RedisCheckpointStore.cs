using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using StackExchange.Redis;

namespace Viter.Consumer.Consumer.Data;

public class RedisCheckpointStore : CheckpointStore
{
    private readonly IDatabase _redis;
    private const string CheckPointKeyTemplate = "Checkpoint:{0}:{1}:{2}";
    private const string PartitionOwnershipTemplate = "PartitionOwnership:{0}:{1}:{2}";

    public RedisCheckpointStore(IDatabase redis)
    {
        _redis = redis;
    }

    public override async Task<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
        string fullyQualifiedNamespace, string eventHubName, string consumerGroup,
        CancellationToken cancellationToken)
    {
        List<EventProcessorPartitionOwnership>? result = null;
        string key = string.Format(PartitionOwnershipTemplate, fullyQualifiedNamespace, eventHubName, consumerGroup);
        HashEntry[] records = await _redis.HashGetAllAsync(key);
        foreach (HashEntry record in records)
        {
            string partitionId = record.Name.ToString();
            if (string.IsNullOrWhiteSpace(partitionId))
            {
                continue;
            }

            PartitionOwnershipValue? ownership = JsonSerializer.Deserialize<PartitionOwnershipValue>(record.Value.ToString());
            if (ownership is null)
            {
                continue;
            }

            (result ??= new()).Add(
                new EventProcessorPartitionOwnership
                {
                    FullyQualifiedNamespace = fullyQualifiedNamespace,
                    EventHubName = eventHubName,
                    ConsumerGroup = consumerGroup,
                    PartitionId = partitionId,
                    LastModifiedTime = ownership.LastModifiedTime,
                    OwnerIdentifier = ownership.OwnerIdentifier,
                    Version = ownership.Version
                });
        }

        return result?.AsEnumerable() ?? Enumerable.Empty<EventProcessorPartitionOwnership>();
    }

    public override async Task<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
        IEnumerable<EventProcessorPartitionOwnership> desiredOwnership, CancellationToken cancellationToken)
    {
        List<EventProcessorPartitionOwnership>? claimedOwnership = null;

        foreach (EventProcessorPartitionOwnership ownership in desiredOwnership)
        {
            string key = string.Format(PartitionOwnershipTemplate, ownership.FullyQualifiedNamespace,
                ownership.EventHubName, ownership.ConsumerGroup);

            if (ownership.Version is not null)
            {
                RedisValue redisValue = await _redis.HashGetAsync(key, ownership.PartitionId);
                if (redisValue.HasValue)
                {
                    PartitionOwnershipValue? existingOwnership =
                        JsonSerializer.Deserialize<PartitionOwnershipValue>(redisValue.ToString());
                    if (existingOwnership is not null)
                    {
                        // update owner and version
                        existingOwnership.OwnerIdentifier = ownership.OwnerIdentifier;
                        existingOwnership.Version = Guid.NewGuid().ToString();
                        existingOwnership.LastModifiedTime = DateTimeOffset.UtcNow;
                        await _redis.HashSetAsync(key, new[]
                        {
                            new HashEntry(ownership.PartitionId, JsonSerializer.Serialize(existingOwnership))
                        });
                        ownership.Version = existingOwnership.Version;
                        ownership.LastModifiedTime = existingOwnership.LastModifiedTime;
                    }
                    else
                    {
                        ownership.Version = null;
                    }
                }
                else
                {
                    ownership.Version = null;
                }

                if (ownership.Version is null)
                {
                    PartitionOwnershipValue newOwnership = new()
                    {
                        OwnerIdentifier = ownership.OwnerIdentifier,
                        Version = Guid.NewGuid().ToString(),
                        LastModifiedTime = DateTimeOffset.UtcNow,
                    };
                    await _redis.HashSetAsync(key,
                        new[] { new HashEntry(ownership.PartitionId, JsonSerializer.Serialize(newOwnership)) });

                    ownership.LastModifiedTime = newOwnership.LastModifiedTime;
                    ownership.Version = newOwnership.Version;
                }
            }

            (claimedOwnership ??= new()).Add(ownership);
        }

        return claimedOwnership?.AsEnumerable() ?? Enumerable.Empty<EventProcessorPartitionOwnership>();
    }

    public override async Task<EventProcessorCheckpoint> GetCheckpointAsync(string fullyQualifiedNamespace,
        string eventHubName, string consumerGroup, string partitionId,
        CancellationToken cancellationToken)
    {
        string key = string.Format(CheckPointKeyTemplate, fullyQualifiedNamespace,
            eventHubName, consumerGroup);
        RedisValue redisValue = await _redis.HashGetAsync(key, partitionId);
        if (redisValue.IsNullOrEmpty)
        {
            return null!;
        }

        CheckpointValue? checkpointValue = JsonSerializer.Deserialize<CheckpointValue>(redisValue.ToString());

        if (checkpointValue is null)
        {
            return null!;
        }

        return new EventProcessorCheckpoint
        {
            FullyQualifiedNamespace = fullyQualifiedNamespace,
            EventHubName = eventHubName,
            ConsumerGroup = consumerGroup,
            PartitionId = partitionId,
            StartingPosition = EventPosition.FromOffset(checkpointValue.Offset)
        };
    }

    public override async Task UpdateCheckpointAsync(string fullyQualifiedNamespace, string eventHubName,
        string consumerGroup,
        string partitionId, long offset, long? sequenceNumber, CancellationToken cancellationToken)
    {
        string key = string.Format(CheckPointKeyTemplate, fullyQualifiedNamespace,
            eventHubName, consumerGroup);
        await _redis.HashSetAsync(key, new[]
        {
            new HashEntry(partitionId, JsonSerializer.Serialize(new CheckpointValue
            {
                Offset = offset,
                SequenceNumber = sequenceNumber
            }))
        });
    }
}