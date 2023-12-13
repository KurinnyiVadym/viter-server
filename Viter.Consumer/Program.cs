using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Options;
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
        .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
        .UseFeatureFlags();
});
var services = builder.Services;
services.AddSingleton<ConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
services.Configure<BatchProcessorOptions>(builder.Configuration.GetSection("BatchProcessor"));
services.AddSingleton<CheckpointStore, RedisCheckpointStore>();
services.AddSingleton<IEventBatchConsumer, TelemetryEventBatchConsumer>();
services.AddSingleton<ITimeSeriesManager, TimeSeriesManager>();
services.AddHostedService<SimpleBatchProcessor>();
//api
services.AddSingleton(
    RegistryManager.CreateFromConnectionString(builder.Configuration["ConnectionString:DeviceRegistryManager"]));

var app = builder.Build();

app.MapGet("/", () => "Hello Consumer!");

await app.RunAsync();