namespace Viter.Consumer.Protocol;

public class TelemetryResponse
{
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public required string DeviceId { get; set; }
    public DateTime Time { get; set; }
}