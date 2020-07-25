using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Reminders;
using System.Threading.Tasks;
using BalderHash;

namespace Mute.Moe.Controllers
{
    [Route("Reminders")]
    public class RemindersController
        : Controller
    {
        private readonly IReminders _reminders;
        private readonly DiscordSocketClient _client;

        public RemindersController(IReminders reminders, DiscordSocketClient client)
        {
            _reminders = reminders;
            _client = client;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["_client"] = _client;
            ViewData["_reminders"] = _reminders;
            return View();
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string strId)
        {
            var discorduser = User.TryGetDiscordUser(_client);
            if (discorduser == null)
                return Unauthorized();

            var id = BalderHash32.Parse(strId);
            if (id == null)
                return BadRequest();

            var success = await _reminders.Delete(discorduser.Id, id.Value.Value);
            if (success)
                return Ok();
            else
                return NotFound();
        }
    }
}