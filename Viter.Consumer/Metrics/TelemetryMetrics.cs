using System.Diagnostics.Metrics;

namespace Viter.Consumer.Metrics;

public class TelemetryMetrics
{
    private readonly Counter<int> _processedTelemetryCount;

    public TelemetryMetrics(IMeterFactory meterFactory)
    {
        Meter meterInstance = meterFactory.Create("Viter.Telemetry");
        _processedTelemetryCount = meterInstance.CreateCounter<int>("viter.telemetry.processed-telemetry-count");
    }

    public void IncreaseProcessedCount(int quantity)
    {
        _processedTelemetryCount.Add(quantity);
    }
}