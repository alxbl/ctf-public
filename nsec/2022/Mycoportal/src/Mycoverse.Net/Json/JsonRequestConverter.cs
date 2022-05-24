namespace Mycoverse.Net.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonRequestConverter : JsonConverter<JsonRequest>
{
    enum RequestType
    {
        ListApiKeys = 1,
        AddApiKey = 2,
        // RemoveApiKey = 3,
        UploadAvatar = 4
    }

    public override bool CanConvert(Type t) => typeof(JsonRequest).IsAssignableFrom(t);

    public override JsonRequest Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions opts)
    {
        var tmp = r;
        if (tmp.TokenType != JsonTokenType.StartObject) throw new JsonException();
        tmp.Read();
        if (tmp.TokenType != JsonTokenType.PropertyName) throw new JsonException();

        string? propertyName = tmp.GetString();
        if (propertyName != "Type") throw new JsonException();

        tmp.Read();
        if (tmp.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        RequestType type = (RequestType)tmp.GetInt32();
        return type switch
        {
            RequestType.AddApiKey => JsonSerializer.Deserialize<AddApiKeyRequest>(ref r)!,
            RequestType.ListApiKeys => JsonSerializer.Deserialize<ListApiKeysRequest>(ref r)!,
            // RequestType.RemoveApiKey => new RemoveApiKeyRequest()
            RequestType.UploadAvatar => JsonSerializer.Deserialize<UploadAvatarRequest>(ref r)!,
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, JsonRequest person, JsonSerializerOptions options) => throw new NotImplementedException();
}