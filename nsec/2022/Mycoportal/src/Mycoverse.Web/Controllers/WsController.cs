namespace Mycoverse.Web.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.WebSockets;
using System.Net.Sockets;

using Mycoverse.Web.Options;
using Microsoft.AspNetCore.Connections;
using System.Text;

[Route("/ws")]
public class WsController : ControllerBase
{
    private ProxyOptions _opts;
    private ILogger<WsController> _log;

    public WsController(IOptions<ProxyOptions> opts, ILogger<WsController> log)
    {
        _opts = opts.Value;
        _log = log;
    }

    [HttpGet]
    public async Task Get(CancellationToken k)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await Proxy(webSocket, k);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task Proxy(WebSocket ws, CancellationToken k) // Propagate cancellation tokens? meh.
    {
        _log.LogDebug("Proxying a connection for {0}", HttpContext.GetEndpoint());
        var buffer = new byte[1024 * 4];
        using var proxy = new TcpClient(_opts.Url, _opts.Port);
        var avatar = proxy.GetStream();
        var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), k);
        try
        {
            while (!receiveResult.CloseStatus.HasValue)
            {
                var inbound = new ArraySegment<byte>(buffer, 0, receiveResult.Count);
                await avatar.WriteAsync(inbound, k);
                var readCount = await avatar.ReadAsync(new ArraySegment<byte>(buffer), k);
                var outbound = new ArraySegment<byte>(buffer, 0, readCount);
                await ws.SendAsync(outbound, WebSocketMessageType.Text, true, k);
                receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), k);
            }

            _log.LogDebug("Closing connection for {0}", HttpContext.GetEndpoint());
            await ws.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription, k);
        }
        catch (ConnectionAbortedException) { } // Bope.
    }
}