using Microsoft.AspNetCore.Mvc.Testing;

namespace Viter.Api.Integration.Test.Fixture;

public class ApiBaseTest : IAsyncLifetime
{
    private WebApplicationFactory<Program> _webApplicationFactory = null!;
    protected HttpClient ViterClient = null!;

    public async Task InitializeAsync()
    {
        _webApplicationFactory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                //to do configurationBuilder
            });
            builder.ConfigureServices(services => {
                //to do
            });
        });
        ViterClient = _webApplicationFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _webApplicationFactory.DisposeAsync();
    }
}
