namespace Mycoverse.Net.Json;

public class JsonRequest
{
    public static JsonRequest? Parse(string line)
    {
        return default;
    }
    
    public string Username { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}