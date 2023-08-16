using NRedisTimeSeries;
using NRedisTimeSeries.Commands.Enums;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;

namespace Viter.Consumer.DataBase;

internal class TimeSeriesManager : ITimeSeriesManager
{
    private readonly IDatabase _redis;
    private readonly ILogger<TimeSeriesManager> _logger;
    private readonly HashSet<string> _deviceKeys = new();

    public TimeSeriesManager(IDatabase redis, ILogger<TimeSeriesManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async ValueTask<string> GetKeyForDevice(string keyPrefix, string deviceId)
    {
        string redisKey = $"{keyPrefix}:{deviceId}";

        if (!_deviceKeys.TryGetValue(redisKey, out _))
        {
            await CreateTimeSeries(redisKey, deviceId);
            _deviceKeys.Add(redisKey);
        }

        return redisKey;
    }

    private async Task CreateTimeSeries(string redisKey, string deviceId)
    {
        long retentionTime = (long)TimeSpan.FromDays(7).TotalMilliseconds;
        try
        {
            if (!await _redis.KeyExistsAsync(redisKey))
            {
                await _redis.TimeSeriesCreateAsync(redisKey, retentionTime,
                    new List<TimeSeriesLabel> { new TimeSeriesLabel("id", deviceId) },
                    duplicatePolicy: TsDuplicatePolicy.LAST);
                _logger.LogInformation("Key {RedisKey} was created.", redisKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on creating TimeSeries for {DeviceID}", deviceId);
        }
    }
}