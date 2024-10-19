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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

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
            if (ops.SslProtocols != null)
            {
                Console.WriteLine("{0}: Update SslProtocols with {1}", ops.Route, ops.SslProtocols);
                handler.SslProtocols = ops.SslProtocols.Value;
            }

            //Accept all Server Certificate
            if (ops.AcceptServerCertificate ==true)
            {
                Console.WriteLine("{0}: AcceptServerCertificate", ops.Route);
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            //Add Client certificate
            if (!string.IsNullOrEmpty(ops.ClientCert))
            {
                Console.WriteLine("{0}: Added ClientCert", ops.Route);

                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
                handler.ClientCertificates.Add(new X509Certificate2(Convert.FromBase64String(ops.ClientCert),
                    ops.ClientCertPass,X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet));
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