using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mute.Moe.Controllers
{
    [Route("Admin")]
    [Authorize("BotOwner")]
    public class AdminController
        : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}