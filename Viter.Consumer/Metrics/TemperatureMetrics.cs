using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Viter.Consumer.Metrics;

public class TemperatureMetrics
{
    private readonly ObservableGauge<double> _temperatureCounter;

    private readonly ConcurrentDictionary<string, Measurement<double>> _temperaturesMeasurements =
        new ConcurrentDictionary<string, Measurement<double>>();

    public TemperatureMetrics(IMeterFactory meterFactory)
    {
        Meter meterInstance = meterFactory.Create("Viter.Telemetry");
        _temperatureCounter = meterInstance.CreateObservableGauge<double>("viter.telemetry.temperature",
            () => _temperaturesMeasurements.Values);
    }

    public void SetTemperature(double temperature, string deviceId)
    {
        _temperaturesMeasurements[deviceId] =
            new Measurement<double>(temperature, new KeyValuePair<string, object?>("deviceId", deviceId));
    }
}