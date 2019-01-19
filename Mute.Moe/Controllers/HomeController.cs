using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mute.Moe.Models;

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

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var sigil = new Sigil.Sigil(unchecked((uint)HttpContext.TraceIdentifier.GetHashCode()));

            return View(new ErrorViewModel {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
