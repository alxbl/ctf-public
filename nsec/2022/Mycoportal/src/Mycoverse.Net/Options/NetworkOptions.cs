namespace Mycoverse.Net.Options;

public class NetworkOptions
{
    public string Listen { get; set; } = "127.0.0.1";
    public ushort Port { get; set; } = 3388;
    public string Proto { get; set; } = "tcp";
    public int Queue { get; set; } = 10;
    public int Concurrent { get; set; } = 25;
}