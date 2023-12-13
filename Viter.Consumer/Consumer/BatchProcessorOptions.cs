namespace Viter.Consumer.Consumer;

public class BatchProcessorOptions
{
    public int EventBatchMaximumCount { get; set; } = 100;
    public string ConsumerGroup { get; set; }
    public string EventHubConnectionString { get; set; }
    public string EventHubName { get; set; }
}