namespace Viter.Api.Endpoints;

public static class EndpointsExtensions
{
    public static RouteGroupBuilder AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGroup("/api")
                .AddDevicesEndpoints()
                .AddTelemetriesndpoints();
    }
}