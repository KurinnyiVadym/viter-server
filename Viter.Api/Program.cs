using Microsoft.Azure.Devices;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using StackExchange.Redis;
using Viter.Api.Endpoints;
var builder = WebApplication.CreateBuilder(args);


string connectionString = builder.Configuration["AzureAppConfig_ConnectionString"];
builder.Configuration.AddAzureAppConfiguration(opt =>
{
    opt.Connect(connectionString)
        .Select(KeyFilter.Any)
        .Select(KeyFilter.Any, builder.Environment.EnvironmentName);
});
builder.Services.AddSingleton<ConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
builder.Services.AddSingleton(
    RegistryManager.CreateFromConnectionString(builder.Configuration["ConnectionString:DeviceRegistryManager"]));
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.AddEndpoints()
.WithOpenApi();

app.Run();
