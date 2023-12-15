namespace Viter.Api.Protocol;

public class TelemetriesResponse
{
    public required List<TelemetryValues> Telemetries { get; set; }
}

public class TelemetryValues
{
    public required DateTime Time { get; set; }
    public required double Temperature { get; set; }
    public required double Humidity { get; set; }
}