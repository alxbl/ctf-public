namespace Mycoverse.Services.Avatar;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mycoverse.Common.Cryptography;
using Mycoverse.Net.Json;
using Mycoverse.Net.Options;
using Mycoverse.Common.Model;
using Mycoverse.Common.Data;

using IJsonServer = Mycoverse.Net.IServer<Mycoverse.Net.Json.JsonConnection, Mycoverse.Net.Json.JsonRequest, Mycoverse.Net.Json.JsonResponse>;
public class AvatarService : BackgroundService
{
    private readonly ILogger<AvatarService> _log;

    private readonly IJsonServer _server;
    private readonly NetworkOptions _opts;
    private readonly ICipher _cipher;
    private readonly ApiKeyService _api;
    private readonly ResourcePool<Session> _pool;

    private readonly AvatarValidator _validator;

    private static readonly Random Rng = new Random();

    public AvatarService(ILogger<AvatarService> log
        , IOptions<NetworkOptions> opts
        , ICipher cipher
        , IJsonServer server
        , ApiKeyService apiKeys
        , AvatarValidator validator
    )
    {
        _log = log;
        _opts = opts.Value;
        _cipher = cipher;
        _server = server;
        _pool = new ResourcePool<Session>(_opts.Concurrent);
        _api = apiKeys;
        _validator = validator;
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        _log.LogInformation("Mycoverse.Services.Avatar starting");
        using var server = _server;

        while (!stopping.IsCancellationRequested)
        {
            var conn = await _server.AcceptAsync(stopping);
            _ = RunAsync(conn, stopping);
        }
        _log.LogInformation("Mycoverse.Services.Avatar stopping");
    }

    private async Task RunAsync(JsonConnection conn, CancellationToken stopping)
    {
        using var _ = conn;
        while (!stopping.IsCancellationRequested && conn.IsAlive)
        {
            var msg = await conn.ReceiveAsync(stopping);
            if (msg == null) break; // Connection closed.

            using var session = await _pool.GetAync(stopping);
            var s = session.Resource;
            s.ApiKey = msg.ApiKey;
            s.Username = msg.Username;

            var t = Process(s, msg, stopping).ContinueWith(async r => await conn.SendAsync(r.Result, stopping))
                .ConfigureAwait(false);
        }
    }

    private async Task<JsonResponse> Process(Session s, JsonRequest msg, CancellationToken k)
    {
        return msg switch
        {
            AddApiKeyRequest req => await GenerateAsync(s, req, k),
            ListApiKeysRequest req => await ListAsync(s, req, k),
            UploadAvatarRequest req => await UploadAsync(s, req, k),

            _ => new JsonResponse(false, "unknown command")
        };
    }

    private async Task<AddApiKeyResponse> GenerateAsync(Session s, AddApiKeyRequest req, CancellationToken k)
    {
        var bytes = new byte[32];
        Rng.NextBytes(bytes);
        var key = BitConverter.ToString(bytes).Replace("-", string.Empty);
        return await _api.AddAsync(s, key, k) switch {
             bool res => new AddApiKeyResponse(key),
             _ => new AddApiKeyResponse()
        };
    }

    private async Task<ListApiKeysResponse> ListAsync(Session s, ListApiKeysRequest req, CancellationToken k)
    {
        var keys = await _api.ListAsync(s, k);

        if (keys is null) return new ListApiKeysResponse();
        var res =  (req.Max > 0) ? keys.Take(req.Max).ToList() : keys;
        return new ListApiKeysResponse(res);
    }

    private async Task<UploadAvatarResponse> UploadAsync(Session s, UploadAvatarRequest req, CancellationToken k)
    {
        if (!await _api.ValidateAsync(s, k)) return new UploadAvatarResponse("Not Authorized");
        if (s.Username == "guest") return new UploadAvatarResponse("User guest cannot upload avatars, please use shiitakoin to purchase an account.");
        if (string.IsNullOrEmpty(req.Data)) return new UploadAvatarResponse("Bad data");
        if (string.IsNullOrEmpty(req.Name)) return new UploadAvatarResponse("Name not specified");
        var avatar = Compression.Decompress(req.Data);

        if (_validator.Validate(avatar))
            File.WriteAllText($"/tmp/{req.Name}", req.Data); // Looks like an arbitrary write, but it's a red herring.

        return new UploadAvatarResponse();
    }
}