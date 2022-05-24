namespace Mycoverse.Web.Controllers;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

using Mycoverse.Common.Model;

[Route("/")]
public class RootController : Controller
{
    private readonly ILogger<RootController> _log;
    public RootController(ILogger<RootController> log)
    {
        _log = log;
    }

    [HttpGet]
    public IActionResult Get()
    {
        ViewData["Title"] = "Home";
        return View("Index");
    }

    [HttpGet("whoami")]
    public IActionResult Whoami()
    {
        var s = HttpContext.Items["S"] as Session;
        return Json(new {Username = s?.Username ?? "guest"});
    }
}