namespace Mycoverse.Net;

/// <summary>Interface for generic servers</summary>
public interface IServer<TConn, TReq, TResp> : IDisposable where TConn : Connection<TReq, TResp>, new()
{
    Task<TConn> AcceptAsync(CancellationToken k);
}
