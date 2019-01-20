using Microsoft.AspNetCore.Mvc;
using HashMedly.Generators.Generator32;
using JetBrains.Annotations;

namespace Mute.Moe.Controllers
{
    [Route("Sigil")]
    public class SigilController
        : Controller
    {
        [HttpGet("/Sigil/{data}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "foreground", "background" })]
        public IActionResult Index([CanBeNull, FromRoute] string data, [CanBeNull, FromQuery] string foreground, [CanBeNull, FromQuery] string background)
        {
            var hash = Murmur3.Create().Mix(data ?? "").Hash;

            return Content(new Sigil.Sigil(hash).ToSvg(foreground ?? "black", background ?? "white").ToString(), "image/svg+xml; charset=utf-8");
        }
    }
}
