// ReSharper disable TemplateIsNotCompileTimeConstantProblem

using System.Text;

namespace HBD.YarpProxy.Configs;

internal class HttpClientWithLogHandler(ILogger<HttpClientWithLogHandler> logger) : HttpClientHandler
{
    public IDictionary<string, string> DefaultRequestHeaders { get; set; } = new Dictionary<string, string>();

    private void TryAddDefaultHeader(HttpRequestMessage request)
    {
        foreach (var header in DefaultRequestHeaders)
        {
            if (request.Headers.Contains(header.Key)) continue;
            request.Headers.Add(header.Key, header.Value);
        }
    }
    
    private void LogRequest(HttpRequestMessage request)
    {
        var message = new StringBuilder($"Forward request to {request.RequestUri}");
        if (ClientCertificates.Count > 0)
            message.Append($" with client cert {ClientCertificates[0].Subject}");
        if (DefaultRequestHeaders.Any())
            message.Append($" with default headers: {string.Join(", ", DefaultRequestHeaders.Select(h => h.Key))}");
        
        logger.LogDebug(message.ToString());
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LogRequest(request);
        
        TryAddDefaultHeader(request);
        return base.Send(request, cancellationToken);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LogRequest(request);

        TryAddDefaultHeader(request);
        return base.SendAsync(request, cancellationToken);
    }
}