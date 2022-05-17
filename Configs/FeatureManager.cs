namespace HBD.YarpProxy.Configs;

public class FeatureOptions
{
    public static string Name => "FeatureManagement";
    
    public bool EnableForwarder { get; set; } = false;
    public bool EnableReverseProxy { get; set; } = true;
    public bool EnableHttpLog { get; set; } = false;
}