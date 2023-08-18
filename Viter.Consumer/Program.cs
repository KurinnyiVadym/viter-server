using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using NRedisTimeSeries;
using StackExchange.Redis;
using Viter.Consumer.Consumer;
using Viter.Consumer.Consumer.Data;
using Viter.Consumer.DataBase;


var builder = WebApplication.CreateBuilder(args);
string connectionString = builder.Configuration["AzureAppConfig_ConnectionString"]!;

// Load configuration from Azure App Configuration
builder.Configuration.AddAzureAppConfiguration(opt =>
{
    opt.Connect(connectionString)
        .Select(KeyFilter.Any)
        .Select(KeyFilter.Any, builder.Environment.EnvironmentName);
});
var services = builder.Services;
services.AddSingleton<ConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
services.AddSingleton<CheckpointStore, RedisCheckpointStore>();
services.AddSingleton<IEventBatchConsumer, TelemetryEventBatchConsumer>();
services.AddSingleton<ITimeSeriesManager, TimeSeriesManager>();
services.AddHostedService<SimpleBatchProcessor>();
//api
services.AddSingleton(
    RegistryManager.CreateFromConnectionString(builder.Configuration["ConnectionString:DeviceRegistryManager"]));
// //
var app = builder.Build();


app.MapGet("/", () => "Hello World!");
app.MapGet("/api/devices", async (RegistryManager registryManager, ITimeSeriesManager timeSeriesManager, IDatabase database) =>
{
    IEnumerable<Device> devices = await registryManager.GetDevicesAsync(100);
    
    return devices.Select(d => new
        { d.Id, d.ConnectionState, d.ConnectionStateUpdatedTime, d.LastActivityTime, d.StatusUpdatedTime, data = database.TimeSeriesGet(timeSeriesManager.GetKeyForDevice("temperature", d.Id).Result) });
});
await app.RunAsync();