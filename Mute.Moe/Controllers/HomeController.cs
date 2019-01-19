using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mute.Moe.Models;
using System.Linq;

namespace Mute.Moe.Controllers
{
    public class HomeController
        : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            ViewData["ID"] = HttpContext.TraceIdentifier;

            if (User.Identities.Any(id => id.AuthenticationType == "Discord"))
                return Redirect("dashboard");
            else
                return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
