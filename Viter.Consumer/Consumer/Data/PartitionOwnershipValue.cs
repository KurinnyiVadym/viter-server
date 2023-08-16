namespace Viter.Consumer.Consumer.Data;

public class PartitionOwnershipValue
{
    public string OwnerIdentifier { get; set; }

    public DateTimeOffset LastModifiedTime { get; set; }

    public string Version { get; set; }
}