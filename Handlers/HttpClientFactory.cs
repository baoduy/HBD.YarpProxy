using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using HBD.YarpProxy.Configs;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace HBD.YarpProxy.Handlers;

public interface IHttpClientFactory
{
    (HttpMessageInvoker httpClient, HttpTransformer transformer) Create(ForwarderOption forwarderOption);
}

public class HttpClientFactory(ITransformBuilder transformBuilder, IServiceProvider serviceProvider)
    : IHttpClientFactory
{
    private readonly ConcurrentDictionary<ForwarderOption, (HttpMessageInvoker httpClient, HttpTransformer transformer)>
        _cache = new();

    public (HttpMessageInvoker httpClient, HttpTransformer transformer) Create(ForwarderOption forwarderOption) =>
        _cache.GetOrAdd(forwarderOption, key =>
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var handler =
                new HttpClientWithLogHandler(serviceProvider
                    .GetRequiredService<ILogger<HttpClientWithLogHandler>>())
                {
                    UseProxy = false,
                    UseCookies = false,
                    AllowAutoRedirect = false,
                    //MaxAutomaticRedirections = 0,

                    AutomaticDecompression = DecompressionMethods.None,
                    DefaultRequestHeaders = forwarderOption.Headers
                };

            //Update SslProtocols
            if (forwarderOption.SslProtocols != null)
            {
                Console.WriteLine("Update SslProtocols of {0} with {1}", forwarderOption.Route,
                    forwarderOption.SslProtocols);
                handler.SslProtocols = forwarderOption.SslProtocols.Value;
            }

            //Accept all Server Certificate
            if (forwarderOption.AcceptServerCertificate)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            //Add Client certificate
            if (!string.IsNullOrEmpty(key.ClientCertificate))
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Add(new X509Certificate2(Convert.FromBase64String(key.ClientCertificate),
                    key.ClientCertificatePassword));
            }

            var httpClient = new HttpMessageInvoker(handler);

            var prefix = key.Route.StartsWith("/")
                ? key.Route.Replace("/{**catch-all}", string.Empty)
                : $"/{key.Route.Replace("/{**catch-all}", string.Empty)}";

            //Transform prefix
            var transformer = transformBuilder.Create(b =>
            {
                if (!prefix.Equals("/", StringComparison.OrdinalIgnoreCase))
                    b.AddPathRemovePrefix(prefix);

                b.AddResponseTransform(t =>
                {
                    t.HeadersCopied = true;
                    return ValueTask.CompletedTask;
                });
            });

            return (httpClient, transformer);
        });
}