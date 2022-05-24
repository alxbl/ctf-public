namespace Mycoverse.Services.Backup;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mycoverse.Common;
using Mycoverse.Common.Cryptography;

using Mycoverse.Net;
using Mycoverse.Services.Backup.Options;

using TextServer = Mycoverse.Net.IServer<Mycoverse.Net.TextConnection, Mycoverse.Net.TextRequest, Mycoverse.Net.TextResponse>;
using TextConnection = Mycoverse.Net.IConnection<Mycoverse.Net.TextRequest, Mycoverse.Net.TextResponse>;

public class BackupService : BackgroundService
{

    private readonly ICipher _cipher;
    private readonly ILogger<BackupService> _log;
    private readonly TextServer _server;
    private readonly BackupOptions _config;

    public BackupService(ILogger<BackupService> logger, ICipher cipher, TextServer server, IOptions<BackupOptions> config)
    {
        _cipher = cipher;
        _log = logger;
        _server = server;
        _config = config.Value;
        _log.LogDebug("Created!");
    }

    protected override async Task ExecuteAsync(CancellationToken stopping)
    {
        _log.LogInformation("Mycoverse.Services.Backup starting");
        using var server = _server;

        while (!stopping.IsCancellationRequested)
        {
            var conn = await _server.AcceptAsync(stopping);
            _ = RunAsync(conn, stopping);
        }
        _log.LogInformation("Mycoverse.Services.Backup stopping");
    }

    private async Task RunAsync(TextConnection conn, CancellationToken stopping)
    {
        using var _ = conn;
        while (!stopping.IsCancellationRequested && conn.IsAlive)
        {
            var msg = await conn.ReceiveAsync(stopping);
            if (msg == null) break; // Connection closed.
            var response = msg.Verb.ToLowerInvariant() switch
            {
                "backup" => Backup(msg),
                "ping" => new TextResponse("Pong!"),
                "bye" => new TextResponse(":("),
                "help" => new TextResponse("commands: backup <path>, ping, bye, help"),
                _ => new TextResponse("Invalid command, try 'help'")
            };

            await conn.SendAsync(response, stopping);
            
            if (msg.Verb.ToLowerInvariant() == "bye") {
                conn.Close();
                break;
            }
        }
    }

    private TextResponse Backup(TextRequest msg) 
    {
        _log.LogInformation("Backup requested for {0}", msg.Body);
        return new TextResponse(Backup(msg.Body));
    }

    string Backup(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "Invalid path";

        var fi = new FileInfo(path);
        if (Directory.Exists(fi.FullName)) return "Directory backup not supported";
        if (fi.LinkTarget != null) return Backup(fi.LinkTarget);
        if (!fi.Exists) return "File does not exist";

        if (_config.IsDenied(fi)) return "Backup not allowed";

        var outfile = new FileInfo(Path.Combine(_config.BackupDir, fi.Name));
        var action = outfile.Exists ? "Overwrote" : "Wrote";

        var src = fi.OpenRead();
        if (outfile.Exists) outfile.Delete();
        var dst = outfile.OpenWrite();
        if (_config.ShouldEncrypt(fi)) 
            _cipher.Encrypt(src, dst);
        else
            src.CopyTo(dst);

        return $"{action} {outfile.FullName}";
    }
}