public sealed class MqttOptions
{
    public string Host { get; set;  } = "192.168.1.197";
    public int Port { get; set; } = 1883;
    public string Topic { get; set; } = "/#";
    public string? Username { get; set; }   
    public string? Password { get; set; }
    public string? ClientId { get; set; } = "aspnet-radar-bridge";
}