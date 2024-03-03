using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Viter.Consumer.Models;

namespace Viter.Consumer.Metrics;

public class TemperatureMetrics
{
    private readonly ConcurrentDictionary<string, Telemetry> _telemetries =
        new();

    public TemperatureMetrics(IMeterFactory meterFactory)
    {
        Meter meterInstance = meterFactory.Create("Viter.Telemetry");

        meterInstance.CreateObservableGauge<double>("viter.telemetry.temperature",
            () => _telemetries.Values.Select(t =>
                new Measurement<double>(t.Temperature, new KeyValuePair<string, object?>("deviceId", t.DeviceId))));
        meterInstance.CreateObservableGauge<double>("viter.telemetry.humidity",
            () => _telemetries.Values.Select(t =>
                new Measurement<double>(t.Humidity, new KeyValuePair<string, object?>("deviceId", t.DeviceId))));
    }

    public void SetTelemetry(Telemetry telemetry)
    {
        _telemetries[telemetry.DeviceId ?? "unknown"] = telemetry;
    }
}