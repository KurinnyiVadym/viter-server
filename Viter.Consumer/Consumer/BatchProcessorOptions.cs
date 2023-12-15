namespace Viter.Consumer.Consumer;

public class BatchProcessorOptions
{
    public int EventBatchMaximumCount { get; set; } = 100;
    public required string ConsumerGroup { get; set; }
    public required string EventHubConnectionString { get; set; }
    public required string EventHubName { get; set; }
}