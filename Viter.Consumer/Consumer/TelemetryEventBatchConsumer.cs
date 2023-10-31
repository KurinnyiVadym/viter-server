﻿using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using NRedisTimeSeries;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;
using Viter.Consumer.DataBase;
using Viter.Consumer.Models;

namespace Viter.Consumer.Consumer;

public class TelemetryEventBatchConsumer : IEventBatchConsumer
{
    private readonly ILogger<TelemetryEventBatchConsumer> _logger;
    private readonly IDatabase _redis;
    private readonly ITimeSeriesManager _timeSeriesManager;

    public TelemetryEventBatchConsumer(ILogger<TelemetryEventBatchConsumer> logger, IDatabase redis, ITimeSeriesManager timeSeriesManager)
    {
        _logger = logger;
        _redis = redis;
        _timeSeriesManager = timeSeriesManager;
    }

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public async Task<EventData?> OnMessagesReceived(IEnumerable<EventData> messages, EventProcessorPartition partition)
    {
        EventData? last = null;
        List<(string, TimeStamp, double)> sequence = new();
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
                sequence.Add((temperatureKey, timeStamp, data.Temperature));
                sequence.Add((humidityKey, timeStamp, data.Humidity));
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }

            last = eventData;
        }

        await _redis.TimeSeriesMAddAsync(sequence);

        return last;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}