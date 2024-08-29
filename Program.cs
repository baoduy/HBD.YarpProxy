using HBD.YarpProxy.Configs;
using HBD.YarpProxy.Handlers;
using IHttpClientFactory = HBD.YarpProxy.Handlers.IHttpClientFactory;

var builder = WebApplication.CreateBuilder(args);
var features = builder.Configuration.GetSection(FeatureOptions.Name).Get<FeatureOptions>() ?? new FeatureOptions();

builder.Services
    .AddCors(o => o.AddDefaultPolicy(c => c.AllowAnyOrigin()));

builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

if (features.EnableReverseProxy)
{
    //Note All the configuration already added no need to config anything here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#default-configuration
    builder.Services
        .AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
}

if (features.EnableForwarder)
{
    builder.Services
        .AddHttpForwarder();
}

var app = builder.Build();

// if (features.EnableHttpLog)
// {
//     app.UseHttpLogging();
// }

if (features.EnableReverseProxy)
{
    Console.WriteLine("Reverse Proxy is enabled.");
    app.MapReverseProxy();
}

if (features.EnableForwarder)
{
    Console.WriteLine("Forwarder is enabled.");
    app.MapForwarderProxy();
}

app.Run();