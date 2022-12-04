using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MQTTnetServer.Controllers;

[Authorize]
[ApiController]
public sealed class ServerController : Controller
{
    [Route("api/v1/server/version")]
    [HttpGet]
    public ActionResult<string> GetVersion()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        return fileVersion.ProductVersion ?? "0.0.0.0";
    }
}