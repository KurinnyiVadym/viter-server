using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Extensions.Options;

namespace Viter.Consumer.Consumer;

public class SimpleBatchProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>, IHostedService
{

    private readonly IEventBatchConsumer _consumer;
    private readonly ILogger<SimpleBatchProcessor> _logger;
    private readonly BatchProcessorOptions _options;

    public SimpleBatchProcessor(CheckpointStore checkpointStore,
            IConfiguration configuration,
            IEventBatchConsumer consumer,
            ILogger<SimpleBatchProcessor> logger,
            IOptions<BatchProcessorOptions> options,
            EventProcessorOptions? clientOptions = default)
            : base(
                checkpointStore,
                options.Value.EventBatchMaximumCount,
                options.Value.ConsumerGroup,
                options.Value.EventHubConnectionString,
                options.Value.EventHubName,
                clientOptions)
    {
        _logger = logger;
        _options = options.Value;
        logger.LogInformation($"Connecting to event hub {options.Value.EventHubName} using consumer group {options.Value.ConsumerGroup}");
        _consumer = consumer;
    }

    public bool ExitOnError { get; }

    protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        try
        {
            EventData? lastEvent = await _consumer.OnMessagesReceived(events, partition);
            if (lastEvent == null)
            {
                return;
            }

            await UpdateCheckpointAsync(
                partition.PartitionId,
                lastEvent.Offset,
                lastEvent.SequenceNumber,
                cancellationToken);
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    $"UpdatedCheckpoint on partitionId {partition.PartitionId} to offset: {lastEvent.Offset}, sequence number: {lastEvent.SequenceNumber}");
            }
        }
        catch (Exception ex)
        {
            EventData? firstEvent = events.FirstOrDefault();
            _logger.LogError(ex, $"An error occurred processing events in partition {partition.PartitionId} from offset {firstEvent?.Offset}, sequence number {firstEvent?.SequenceNumber}");
            if (ExitOnError)
            {
                Environment.Exit(1);
            }
        }
    }

    protected override Task OnProcessingErrorAsync(Exception exception,
        EventProcessorPartition partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, $"An error occurred processing events in partition {partition?.PartitionId}. The operation description was '{operationDescription}'.");
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _consumer.StartAsync();
            await StartProcessingAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error starting processing.");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.StopAsync();
        await StopProcessingAsync(cancellationToken);
    }
}