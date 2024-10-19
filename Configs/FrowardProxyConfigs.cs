using System.Security.Authentication;
using Yarp.ReverseProxy.Forwarder;

namespace HBD.YarpProxy.Configs;

public class FrowardProxyItem
{
    public string Route { get; set; } = default!;
    public string Destination { get; set; } = default!;
    public Dictionary<string, string> Headers { get; set; } = new();

    public string? ClientCert { get; set; }
    public string? ClientCertPass { get; set; }

    public SslProtocols? SslProtocols { get; set; }

    public bool? AcceptServerCertificate { get; set; }
}

public class FrowardProxyOps : List<FrowardProxyItem>
{
    public static string Name => "ForwarderProxy";
}

public static class FrowardProxyConfigs
{
    private static IEndpointRouteBuilder MapForwarderProxyEndPoints(this IEndpointRouteBuilder endpoints)
    {
        var logger = endpoints.ServiceProvider.GetRequiredService<ILogger<FrowardProxyOps>>();
        var config = endpoints.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection(FrowardProxyOps.Name)
            .Get<FrowardProxyOps>();

        if (config == null || !config.Any()) return endpoints;


        var forwarder = endpoints.ServiceProvider.GetRequiredService<IHttpForwarder>();
        var httpClientFactory = endpoints.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        // Setup our own request transform class

        var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(200) };

        foreach (var op in config)
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
                        logger.LogError(exception, $"Error at {op.Destination} is {exception.Message}");
                }
            });
        }

        return endpoints;
    }

    public static WebApplication MapForwarderProxy(this WebApplication application)
    {
        application.MapForwarderProxyEndPoints();
        return application;
    }
}