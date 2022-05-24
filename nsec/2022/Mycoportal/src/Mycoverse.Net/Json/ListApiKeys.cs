using Mycoverse.Net.Json;

public class ListApiKeysRequest : JsonRequest
{
    public int Max {get; set;}
}

public class ListApiKeysResponse : JsonResponse
{
    public IList<string> Keys { get; set; } = new List<string>();

    public ListApiKeysResponse(IList<string> keys) : base(true)
    {
        Keys = keys;
    }

    public ListApiKeysResponse() : base(false, "Not authorized") {}
}