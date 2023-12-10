using Microsoft.Azure.Devices;
using Viter.Api.Protocol;

namespace Viter.Api.Endpoints;

public static class DevicesEndpoints
{
    public static RouteGroupBuilder AddDevicesEndpoints(this RouteGroupBuilder root)
    {
        root.MapGet("/devices", async (RegistryManager registryManager) =>
        {
            IEnumerable<Device> devices = await registryManager.GetDevicesAsync(100);

            List<DeviceResponse> devicesList = devices.Select(d => new DeviceResponse
            {
                Id = d.Id,
                ConnectionState = d.ConnectionState,
                ConnectionStateUpdatedTime = d.ConnectionStateUpdatedTime,
                LastActivityTime = d.LastActivityTime,
                StatusUpdatedTime = d.StatusUpdatedTime
            }).ToList();
            return new DevicesResponse { Devices = devicesList };
        }).WithName("Devices");
        return root;
    }
}