using System.Security.Authentication;

namespace HBD.YarpProxy.Configs;

public class ForwarderProxyOptions : List<ForwarderOption>
{
    public static string Name => "ForwarderProxy";
}

public record ForwarderOption
{
    /// <summary>
    /// The route pattern ex: /{**catch-all}
    /// </summary>
    public string Route { get; set; } = default!;
    
    /// <summary>
    /// The destinations to forward to.
    /// </summary>
    public string[] Destinations { get; set; } = default!;

    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string? ClientCertificate { get; set; }
    public string? ClientCertificatePassword { get; set; }
    
    public SslProtocols? SslProtocols { get; set; }
    
    public bool AcceptServerCertificate { get; set; }
    
    internal bool IsValid=> !string.IsNullOrWhiteSpace(Route) && Destinations?.Length > 0 && Destinations.All(d=>!string.IsNullOrWhiteSpace(d));
}