namespace Mycoverse.Net;

using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mycoverse.Net.Options;

public class Server<TConn, TReq, TResp> : IServer<TConn, TReq, TResp> where TConn : Connection<TReq, TResp>, new()
{
    private readonly NetworkOptions _options;
    private readonly ILogger<Server<TConn, TReq, TResp>> _log;
    private readonly IServiceProvider _services;

    private Socket _sock;

    private readonly ISet<Connection<TReq, TResp>> _connections = new HashSet<Connection<TReq, TResp>>();

    public Server(IOptions<NetworkOptions> options, ILogger<Server<TConn, TReq, TResp>> logger, IServiceProvider svc)
    {
        _options = options.Value;
        _log = logger;
        _services = svc;

        _sock = _options.Proto switch // TODO: ServerLoop here to make the API uniform.
        {
            "tcp" => new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp),
            "udp" => throw new NotImplementedException("UDP not supported yet"), // new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp),
            _ => throw new ArgumentException("Unsupported server protocol")
        };

        _sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        Start();
    }

    public async Task<TConn> AcceptAsync(CancellationToken k)
    {
        var remote = await _sock.AcceptAsync();
        var conn = new TConn();
        conn.Initialize(remote, _services.GetService<ILogger<TConn>>() ?? throw new InvalidOperationException("No logger service available."));
        _log.LogDebug("Accepted connection from {0}", remote.RemoteEndPoint);
        lock(_connections) _connections.Add(conn);
        conn.Closed += OnConnectionClosed;
        return conn;
    }

    protected Socket Local => _sock;

    public void Dispose()
    {
        _sock.Dispose();
        lock (_connections)
        {
            foreach (var conn in _connections) conn.Dispose();
            _connections.Clear();
        }
    }

    private void Start()
    {
        var ip = IPAddress.Parse(_options.Listen);
        _sock.Bind(new IPEndPoint(ip, _options.Port));
        _sock.Listen(_options.Queue);
        _log.LogInformation("Listening on {0}:{1} (Queue Size: {2})", ip, _options.Port, _options.Queue);
    }

    private void OnConnectionClosed(object? sender, EventArgs empty)
    {
        var conn = sender as Connection<TReq, TResp>;
        if (conn is null) return;
        conn.Closed -= OnConnectionClosed;
        lock (_connections) _connections.Remove(conn);
    }
}