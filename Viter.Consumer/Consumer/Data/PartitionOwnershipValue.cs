namespace Viter.Consumer.Consumer.Data;

public class PartitionOwnershipValue
{
    public required string OwnerIdentifier { get; set; }

    public required DateTimeOffset LastModifiedTime { get; set; }

    public required string Version { get; set; }
}