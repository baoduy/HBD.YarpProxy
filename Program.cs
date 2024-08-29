using System.Security.Cryptography.X509Certificates;
using HBD.YarpProxy.Configs;
using IHttpClientFactory = HBD.YarpProxy.Configs.IHttpClientFactory;

const string clientCertKey = "ClientCert";
const string clientCertPass = "ClientCertPass";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IHttpClientFactory, HttpClientFactory>();

//Add ReverseProxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        if (context.NewMetadata == null || !context.NewMetadata.TryGetValue(clientCertKey, out var cert64)) return;
        context.NewMetadata.TryGetValue(clientCertPass, out var pass);
        var cert = new X509Certificate2(Convert.FromBase64String(cert64), pass);

        handler.SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
        handler.SslOptions.ClientCertificates ??= new X509CertificateCollection();
        handler.SslOptions.ClientCertificates.Add(cert);
    });

var app = builder.Build();
app.MapReverseProxy();
app.MapForwarderProxy();
app.Run();