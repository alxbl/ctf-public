namespace Mycoverse.Net;

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

public abstract class Connection<TReq, TResp> : IConnection<TReq, TResp>
{
    private readonly byte[] _recv = new byte[4096];
    private ILogger<Connection<TReq, TResp>>? _log;

    public bool IsAlive => Socket?.Connected ?? false;

    public EndPoint Local => Socket!.LocalEndPoint!;

    public EndPoint Remote => Socket!.RemoteEndPoint!;

    public event EventHandler? Closed;

    public virtual async Task<TReq?> ReceiveAsync(CancellationToken k)
    {
        try
        {
            TReq? received = default;
            int size = 0;
            do
            {
                size = await Socket!.ReceiveAsync(_recv, SocketFlags.None, k);
                if (size > 0) received = OnReceive(_recv, size);
            } while (!k.IsCancellationRequested && received == null && size > 0);

            if (size == 0)
            {
                Close(false);
            }

            k.ThrowIfCancellationRequested();
            return received; // Won't be null unless k is cancelled, which throws above.
        }
        catch (Exception)
        {
            Close(false);
            return default; // Disconnected during receive.
        }
    }

    public virtual async Task<bool> SendAsync(TResp msg, CancellationToken k)
    {
        var send = OnSend(msg);

        try
        {
            var sent = 0;
            while (sent < send.Length)
                sent += await Socket!.SendAsync(send, SocketFlags.None, k);
            return true;
        }
        catch (Exception)
        {
            Close(false);
            return false; // Disconnected during send.
        }
    }

    public virtual void Close()
    {
        Close(local: true);
    }

    internal void Initialize(Socket remote, ILogger<Connection<TReq, TResp>> log)
    {
        Socket = remote;
        _log = log;
    }

    public void Dispose() => Close(true);

    protected Socket? Socket { get; private set; }

    protected void Close(bool local) {
        if (Socket is null) return;
        _log?.LogDebug(local ? "Connection closed by server ({0})" : "Connection closed by remote ({0})", Socket.RemoteEndPoint);
        Socket.Close();
        Socket = null;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called when the server receives data from the client </summary>
    /// <returns>A <typeparam tref="TReq" /> if a whole message could be parsed, or null if data is missing.</returns> 
    protected abstract TReq? OnReceive(byte[] buf, int size);

    protected abstract byte[] OnSend(TResp msg);
}
