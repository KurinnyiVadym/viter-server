using Microsoft.Azure.Devices;

namespace Viter.Api.Protocol;

public class DevicesResponse
{
    public List<DeviceResponse> Devices { get; set; }
}

public class DeviceResponse
{
    public string Id { get; set; }
    public DeviceConnectionState ConnectionState { get; set; }
    public DateTime ConnectionStateUpdatedTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public DateTime StatusUpdatedTime { get; set; }
}
