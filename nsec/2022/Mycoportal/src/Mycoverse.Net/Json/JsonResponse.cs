namespace Mycoverse.Net.Json;

public class JsonResponse
{
    public JsonResponse(bool success, string? message = null)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }

    public string? Message {get;}
}
