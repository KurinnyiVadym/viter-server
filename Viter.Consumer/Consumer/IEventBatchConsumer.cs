using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;

namespace Viter.Consumer.Consumer;

public interface IEventBatchConsumer
{
    Task StartAsync();
    Task<EventData?> OnMessagesReceived(IEnumerable<EventData> messages, EventProcessorPartition partition);
    Task StopAsync();
}