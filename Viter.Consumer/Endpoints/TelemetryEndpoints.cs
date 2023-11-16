using Microsoft.Azure.Devices;
using NRedisTimeSeries;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;
using Viter.Consumer.DataBase;
using Viter.Consumer.Protocol;

namespace Viter.Consumer.Endpoints;

public static class TelemetryEndpoints
{
    public static RouteGroupBuilder AddTelemetriesndpoints(this RouteGroupBuilder root)
    {
        root.MapGet("/telemetry/{deviceId}/latest", async (RegistryManager registryManager, ITimeSeriesManager timeSeriesManager, IDatabase database, string deviceId) =>
        {

            Device? device = await registryManager.GetDeviceAsync(deviceId);

            if (device is null)
            {
                return Results.NotFound();
            }

            IReadOnlyList<(string key, IReadOnlyList<TimeSeriesLabel> labels, TimeSeriesTuple value)> timeSeries = await database.TimeSeriesMGetAsync(new List<string>() { $"id={deviceId}" }, false);

            TelemetryResponse telemetry = new()
            {
                DeviceId = deviceId,
                Time = DateTimeOffset.FromUnixTimeMilliseconds((long)timeSeries.First(ts => ts.key.Contains("temperature")).value.Time).DateTime,
                Temperature = timeSeries.First(ts => ts.key.Contains("temperature")).value.Val,
                Humidity = timeSeries.First(ts => ts.key.Contains("humidity")).value.Val
            };

            return Results.Ok(telemetry);
        });

        root.MapGet("/telemetry/{deviceId}", async (RegistryManager registryManager, ITimeSeriesManager timeSeriesManager, IDatabase database,
        string deviceId, DateTime? from, DateTime? to) =>
        {
            to ??= DateTime.UtcNow;
            from ??= to.Value.AddHours(-10);

            Device? device = await registryManager.GetDeviceAsync(deviceId);

            if (device is null)
            {
                return Results.NotFound();
            }

            var res = await database.TimeSeriesMRevRangeAsync(
            new DateTimeOffset(from.Value, TimeSpan.Zero).ToUnixTimeMilliseconds(), new DateTimeOffset(to.Value, TimeSpan.Zero).ToUnixTimeMilliseconds()
            , new[]{$"id={deviceId}"});
            //todo use model

            return Results.Ok(res);
        });
        return root;
    }
}