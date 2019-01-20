using System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Mute.Moe.Controllers
{
    [Route("Error")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _log;

        public ErrorController(ILogger<ErrorController> log)
        {
            _log = log;
        }

        

        [Route("401")]
        public IActionResult ErrorUnauthorized([FromQuery] string returnUrl = "/")
        {
            ViewData["returnUrl"] = Uri.EscapeUriString(returnUrl);
            return View("Error401");
        }

        [Route("404")]
        public IActionResult ErrorNotFound()
        {
            return View("Error404");
        }

        [Route("500")]
        public IActionResult ErrorInternalServerError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _log.LogError(exceptionHandlerPathFeature.Error, exceptionHandlerPathFeature.Error.Message);

            return View("Error501");
        }
    }
}