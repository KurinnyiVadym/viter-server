using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Viter.Consumer.Models;

namespace Viter.Consumer.Metrics;

public class TemperatureMetrics
{
    private readonly int _metricsTtl;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, Telemetry> _telemetries = new();

    private IEnumerable<Telemetry> LatestTelemetries =>
        _telemetries.Values.Where(t => _timeProvider.GetUtcNow().ToUnixTimeSeconds() - t.TimeStamp < _metricsTtl);
    public TemperatureMetrics(IMeterFactory meterFactory, TimeProvider timeProvider, IConfiguration configuration)
    {
        _timeProvider = timeProvider;
        _metricsTtl = configuration.GetValue<int>("metrics_ttl", 60);
        Meter meterInstance = meterFactory.Create("Viter.Telemetry");

        meterInstance.CreateObservableGauge<double>("viter.telemetry.temperature",
            () => LatestTelemetries.Select(t =>
                new Measurement<double>(t.Temperature, new KeyValuePair<string, object?>("deviceId", t.DeviceId))));
        meterInstance.CreateObservableGauge<double>("viter.telemetry.humidity",
            () => LatestTelemetries.Select(t =>
                new Measurement<double>(t.Humidity, new KeyValuePair<string, object?>("deviceId", t.DeviceId))));
        meterInstance.CreateObservableGauge<double>("viter.telemetry.pressure",
            () => LatestTelemetries.Where(t=> t.Pressure.HasValue).Select(t =>
                new Measurement<double>(t.Pressure.GetValueOrDefault(), new KeyValuePair<string, object?>("deviceId", t.DeviceId))));
    }

    public void SetTelemetry(Telemetry telemetry)
    {
        _telemetries[telemetry.DeviceId ?? "unknown"] = telemetry;
    }
}