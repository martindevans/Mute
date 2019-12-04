using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mute.Moe.Controllers
{
    public class DashboardController
        : Controller
    {
        private readonly DiscordSocketClient _client;

        public DashboardController(DiscordSocketClient client)
        {
            _client = client;
        }

        [Authorize("InAnyBotGuild")]
        public IActionResult Index()
        {
            ViewData["_client"] = _client;
            return View();
        }
    }
}