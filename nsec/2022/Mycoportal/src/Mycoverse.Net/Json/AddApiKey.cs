namespace Mycoverse.Net.Json;

public class AddApiKeyRequest : JsonRequest
{
}

public class AddApiKeyResponse : JsonResponse
{
    public string? NewKey {get; set;}
    public AddApiKeyResponse(string key) : base(true, "Please keep the key secret and safe.")
    {
        NewKey = key;
    }

    public AddApiKeyResponse() : base(false, "Not authorized") {}
}