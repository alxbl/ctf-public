
namespace Mycoverse.Common.Model;

using Mycoverse.Common.Data;

public class Session
{
    public string Username { get; set; } = "guest";

    public string? ApiKey { get; set; } = "guest";

    public bool Debug { get; set; }

    public string Description { get; set; } = "FLAG-d9ab19f2733720635a80a238e1388d74b75d6f99"; // Hint 1

    public string Hmac { get; set; } = string.Empty;

    public static Session Restore(string cookie)
    {
        var bytes  = Compression.Decompress(cookie);
        // Deserialize.
        var src = new MemoryStream(bytes);
        return new KaenSerializer().Deserialize<Session>(src) ?? throw new InvalidDataException("invalid cookie");
    }

    public string Save() 
    {
        // Serialize.
        var dst = new MemoryStream(4096);
        new KaenSerializer().Serialize(dst, this);
        dst.Seek(0, SeekOrigin.Begin);
        return Compression.Compress(dst.GetBuffer()[..(int)dst.Length]);
    }
}