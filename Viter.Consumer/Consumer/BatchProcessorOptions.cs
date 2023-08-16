namespace Viter.Consumer.Consumer;

public class BatchProcessorOptions
{
    int EventBatchMaximumCount { get; set; }
    string ConsumerGroup { get; set; }
    string EventHubConnectionString { get; set; }
    string EventHubName { get; set; }
    bool ExitOnError { get; set; }
}