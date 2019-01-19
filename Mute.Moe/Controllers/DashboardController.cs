using Microsoft.AspNetCore.Mvc;

namespace Mute.Moe.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}