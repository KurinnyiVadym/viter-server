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
    
            return devices.Select(d => new
                { d.Id, d.ConnectionState, d.ConnectionStateUpdatedTime, d.LastActivityTime, d.StatusUpdatedTime, data = new
                {
                    temperature= database.TimeSeriesGet(timeSeriesManager.GetKeyForDevice("temperature", d.Id).Result),
                    humidity= database.TimeSeriesGet(timeSeriesManager.GetKeyForDevice("humidity", d.Id).Result),
                } });
        });
        return root;
    }
}