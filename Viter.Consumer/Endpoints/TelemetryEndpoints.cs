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

            //await database.TimeSeriesMGetAsync(new List<string>() { $"id={deviceId}" }, true);
            TimeSeriesTuple[] timeSeries = await Task.WhenAll(
                database.TimeSeriesGetAsync(await timeSeriesManager.GetKeyForDevice("temperature", deviceId)),
                database.TimeSeriesGetAsync(await timeSeriesManager.GetKeyForDevice("humidity", deviceId)));

            TelemetryResponse telemetry = new()
            {
                DeviceId = deviceId,
                Time = timeSeries[0].Time,
                Temperature = timeSeries[0].Val,
                Humidity = timeSeries[1].Val
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

            var res = await database.TimeSeriesRangeAsync(await timeSeriesManager.GetKeyForDevice("temperature", deviceId), from, to);



            return Results.Ok(res);
        });
        return root;
    }
}