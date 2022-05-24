namespace Mycoverse.Net;

using System.Net;

public interface IConnection<TReq, TResp> : IDisposable
{
    bool IsAlive { get; }
    EndPoint Local { get; }

    EndPoint Remote { get; }

    Task<TReq?> ReceiveAsync(CancellationToken k);

    Task<bool> SendAsync(TResp msg, CancellationToken k);

    void Close();
}