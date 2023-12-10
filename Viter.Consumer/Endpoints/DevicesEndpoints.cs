using Microsoft.Azure.Devices;
using NRedisTimeSeries;
using StackExchange.Redis;
using Viter.Consumer.DataBase;

namespace Viter.Consumer.Endpoints;

public static class DevicesEndpoints
{
    public static RouteGroupBuilder AddDevicesEndpoints(this RouteGroupBuilder root)
    {
        root.MapGet("/devices", async (RegistryManager registryManager, ITimeSeriesManager timeSeriesManager, IDatabase database) =>
        {

            IEnumerable<Device> devices = await registryManager.GetDevicesAsync(100);

            var tasks = devices.AsParallel().Select(async d => new
            {
                d.Id,
                d.ConnectionState,
                d.ConnectionStateUpdatedTime,
                d.LastActivityTime,
                d.StatusUpdatedTime
            }).ToList();

            await Task.WhenAll();

            var result = tasks.Select(t => t.Result);
            return result;
        });
        return root;
    }
}