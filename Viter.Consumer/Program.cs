using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using StackExchange.Redis;
using Viter.Consumer.Consumer;
using Viter.Consumer.Consumer.Data;
using Viter.Consumer.DataBase;
using Viter.Consumer.Endpoints;


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
app.AddEndpoints();

await app.RunAsync();