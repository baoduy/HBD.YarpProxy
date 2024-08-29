using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace HBD.YarpProxy.Configs;

public interface IHttpClientFactory
{
    (HttpMessageInvoker httpClient, HttpTransformer transformer) Create(FrowardProxyItem forwarderOption);
}

public class HttpClientFactory(ITransformBuilder transformBuilder, IServiceProvider serviceProvider)
    : IHttpClientFactory
{
    private readonly ConcurrentDictionary<string, (HttpMessageInvoker httpClient, HttpTransformer transformer)>
        _cache = new();

    public (HttpMessageInvoker httpClient, HttpTransformer transformer) Create(FrowardProxyItem ops) =>
        _cache.GetOrAdd(ops.Route, k =>
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
                    DefaultRequestHeaders = ops.Headers
                };

            //Update SslProtocols
            // if (forwarderOption.SslProtocols != null)
            // {
            //     Console.WriteLine("Update SslProtocols of {0} with {1}", forwarderOption.Route,
            //         forwarderOption.SslProtocols);
            //     handler.SslProtocols = forwarderOption.SslProtocols.Value;
            // }
            //
            // //Accept all Server Certificate
            // if (forwarderOption.AcceptServerCertificate)
            // {
            //     handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            // }

            //Add Client certificate
            if (!string.IsNullOrEmpty(ops.ClientCert))
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Add(new X509Certificate2(Convert.FromBase64String(ops.ClientCert),
                    ops.ClientCertPass));
            }

            var httpClient = new HttpMessageInvoker(handler);

            var prefix = ops.Route.StartsWith("/")
                ? ops.Route.Replace("/{**catch-all}", string.Empty)
                : $"/{ops.Route.Replace("/{**catch-all}", string.Empty)}";

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