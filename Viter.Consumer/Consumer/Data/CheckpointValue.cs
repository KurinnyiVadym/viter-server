namespace Viter.Consumer.Consumer.Data;

public class CheckpointValue
{
    public long Offset { get; set; }
    
    public long? SequenceNumber { get; set; }
}