namespace Viter.Api.Protocol;

public class TelemetriesResponse
{
    public List<TelemetryValues> Telemetries { get; set; }
}

public class TelemetryValues
{
    public DateTime Time { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
}