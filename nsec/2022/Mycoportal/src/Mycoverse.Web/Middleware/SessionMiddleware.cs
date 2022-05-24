namespace Mycoverse.Web.Middleware;

using Microsoft.AspNetCore.Http;
using Mycoverse.Common.Model;

public class SessionMiddleware : IMiddleware
{

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate req)
    {
        Session s;
        var append = false;
        if (!ctx.Request.Cookies.TryGetValue("S", out var sessData) || string.IsNullOrWhiteSpace(sessData))
        {
            s = new Session();
            ctx.Items["S"] = s;
            append = true;
        }
        else s = Session.Restore(sessData);

        if (append) ctx.Response.Cookies.Append("S", s.Save());

        await req.Invoke(ctx);

    }

}