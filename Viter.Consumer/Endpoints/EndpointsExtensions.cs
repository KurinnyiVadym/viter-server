namespace Viter.Consumer.Endpoints;

public static class EndpointsExtensions
{
    public static void AddEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api")
            .AddDevicesEndpoints();
    }
}