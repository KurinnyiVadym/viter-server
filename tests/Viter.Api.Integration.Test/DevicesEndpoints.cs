using System.Net.Http.Json;
using Viter.Api.Integration.Test.Fixture;
using Viter.Api.Protocol;

namespace Viter.Api.Integration.Test;

public class DevicesEndpoints : ApiBaseTest
{
    // [Fact]
    public async Task Test1()
    {
        DevicesResponse? response = await ViterClient.GetFromJsonAsync<DevicesResponse>("/api/devices");
        Assert.NotNull(response);

    }
}