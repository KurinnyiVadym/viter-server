using Azure.Messaging.EventHubs.Primitives;
using HealthChecks.UI.Client;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using StackExchange.Redis;
using Viter.Consumer.Consumer;
using Viter.Consumer.Consumer.Data;
using Viter.Consumer.DataBase;
using Viter.Consumer.Metrics;

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
services.AddHealthChecks()
.AddRedis(builder.Configuration.GetConnectionString("Redis")!)
.AddAzureIoTHub(opt =>
{
    opt.AddConnectionString(builder.Configuration.GetConnectionString("DeviceRegistryManager")!)
    .AddRegistryReadCheck();
});
services
.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30);
    options.AddHealthCheckEndpoint("Healthcheck API", "/healthcheck");
}).AddInMemoryStorage();

services.AddSingleton<ConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
services.Configure<BatchProcessorOptions>(builder.Configuration.GetSection("BatchProcessor"));
services.AddSingleton<CheckpointStore, RedisCheckpointStore>();
services.AddSingleton<IEventBatchConsumer, TelemetryEventBatchConsumer>();
services.AddSingleton<ITimeSeriesManager, TimeSeriesManager>();
services.AddSingleton<TelemetryMetrics>();
services.AddSingleton<TemperatureMetrics>();
services.AddHostedService<SimpleBatchProcessor>();
services.AddSingleton(RegistryManager.CreateFromConnectionString(builder.Configuration.GetConnectionString("DeviceRegistryManager")));

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        builder.AddMeter("Viter.Telemetry");
        builder.AddView("http.server.request.duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();
app.MapGet("/", () => "Hello Consumer!");
app.MapHealthChecks("/healthcheck", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecksUI(options => options.UIPath = "/dashboard");

await app.RunAsync();