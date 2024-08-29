using Yarp.ReverseProxy.Forwarder;
using IHttpClientFactory = HBD.YarpProxy.Handlers.IHttpClientFactory;

namespace HBD.YarpProxy.Configs;

internal static class AppExtensions
{
    private static IEndpointRouteBuilder MapForwarderProxyEndPoints(this IEndpointRouteBuilder endpoints)
    {
        var config = endpoints.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection(ForwarderProxyOptions.Name)
            .Get<ForwarderProxyOptions>();

        if (config==null|| !config.Any()) return endpoints;

        var logger = endpoints.ServiceProvider.GetRequiredService<ILogger<ForwarderOption>>();
        var forwarder = endpoints.ServiceProvider.GetRequiredService<IHttpForwarder>();
        var httpClientFactory = endpoints.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        // Setup our own request transform class

        var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(200) };

        foreach (var op in config.Where(c=>c.IsValid))
        {
            endpoints.Map(op.Route, async httpContext =>
            {
                var (httpClient, transformer) = httpClientFactory.Create(op);

                var error = await forwarder.SendAsync(httpContext, op.Destination, httpClient, requestOptions,
                    transformer);

                // Check if the proxy operation was successful
                if (error != ForwarderError.None)
                {
                    var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                    var exception = errorFeature?.Exception?.InnerException ?? errorFeature?.Exception;

                    if (exception != null)
                        logger.LogError(exception,$"Error at {op.Destination} is {exception.Message}");
                }
            });
        }

        return endpoints;
    }

    public static WebApplication MapForwarderProxy(this WebApplication application)
    {
        application
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapForwarderProxyEndPoints());
        return application;
    }
}