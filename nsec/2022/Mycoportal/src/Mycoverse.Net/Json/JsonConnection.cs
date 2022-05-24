namespace Mycoverse.Net.Json;

using Mycoverse.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Sockets;
using System.Threading.Channels;

public class JsonConnection : Connection<JsonRequest, JsonResponse>
{
    private MemoryStream _recv = new MemoryStream();
    private readonly JsonSerializerOptions _opts;
    private NetworkStream? _s;
    private ChannelReader<JsonRequest>? _r;

    public JsonConnection()
    {
        _opts = new JsonSerializerOptions();
        _opts.Converters.Add(new JsonRequestConverter());
    }


    public override async Task<JsonRequest?> ReceiveAsync(CancellationToken k)
    {
        try
        {
            _s ??= new NetworkStream(Socket!);
            _r ??= JsonStream.DeserializeToChannel<JsonRequest>(_s, _opts, k);
            return await _r.ReadAsync(k);
        }
        catch
        {
            // Remote closed connection so the stream has ended.
        }

        Close(false); // Connection closed by remote.
        return null;
    }
    protected override JsonRequest? OnReceive(byte[] buf, int size) => null; // Unused.

    protected override byte[] OnSend(JsonResponse msg) {
        var txt = JsonSerializer.SerializeToUtf8Bytes<object>(msg);
        return txt.Append((byte)'\n').ToArray();
    }
}