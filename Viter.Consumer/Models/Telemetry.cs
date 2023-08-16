namespace Viter.Consumer.Models;

public class Telemetry
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public string? DeviceId { get; set; }
    public long? TimeStamp { get; set; }
}