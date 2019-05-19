using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mute.Moe.Controllers
{
    [Route("Reminders")]
    public class RemindersController
        : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}