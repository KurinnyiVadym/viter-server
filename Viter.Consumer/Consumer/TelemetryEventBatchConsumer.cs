using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using NRedisTimeSeries;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;
using Viter.Consumer.DataBase;
using Viter.Consumer.Metrics;
using Viter.Consumer.Models;

namespace Viter.Consumer.Consumer;

public class TelemetryEventBatchConsumer : IEventBatchConsumer
{
    private readonly ILogger<TelemetryEventBatchConsumer> _logger;
    private readonly IDatabase _redis;
    private readonly ITimeSeriesManager _timeSeriesManager;
    private readonly TelemetryMetrics _telemetryMetrics;
    private readonly TemperatureMetrics _temperatureMetrics;

    public TelemetryEventBatchConsumer(ILogger<TelemetryEventBatchConsumer> logger, IDatabase redis, ITimeSeriesManager timeSeriesManager, TelemetryMetrics telemetryMetrics, TemperatureMetrics temperatureMetrics)
    {
        _logger = logger;
        _redis = redis;
        _timeSeriesManager = timeSeriesManager;
        _telemetryMetrics = telemetryMetrics;
        _temperatureMetrics = temperatureMetrics;
    }

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<EventData?> OnMessagesReceived(IEnumerable<EventData> messages, EventProcessorPartition partition)
    {
        EventData? last = null;
        foreach (EventData eventData in messages)
        {
            try
            {
                Telemetry? data = eventData.EventBody.ToObjectFromJson<Telemetry?>();
                if (data is null)
                {
                    _logger.LogWarning("Unexpected EventBody, couldn't deserialize");
                    continue;
                }

                _logger.LogInformation("{TimeStamp} {DeviceId}, T: {Temperature}, H: {Humidity}",
                    DateTimeOffset.FromUnixTimeSeconds(data.TimeStamp.GetValueOrDefault()), data.DeviceId, data.Temperature, data.Humidity);

                if (string.IsNullOrWhiteSpace(data.DeviceId))
                {
                    _logger.LogWarning("Unknown device send message: {Json}", JsonSerializer.Serialize(data));
                    continue;
                }
                
                if (!data.TimeStamp.HasValue)
                {
                    _logger.LogInformation("Device {DeviceId} sends message without TimeStamp", data.DeviceId);
                }

                TimeStamp timeStamp = data.TimeStamp.HasValue? new TimeStamp(data.TimeStamp.Value * 1000) : "*";

                string temperatureKey = await _timeSeriesManager.GetKeyForDevice("temperature", data.DeviceId);
                string humidityKey = await _timeSeriesManager.GetKeyForDevice("humidity", data.DeviceId);
                await _redis.TimeSeriesMAddAsync(new List<(string, TimeStamp, double)>
                {
                    (temperatureKey, timeStamp, data.Temperature),
                    (humidityKey, timeStamp, data.Humidity)
                });
                _telemetryMetrics.IncreaseProcessedCount(1);
                _temperatureMetrics.SetTemperature(data.Temperature, data.DeviceId);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }

            last = eventData;
        }

        return last;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}