using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Discord.WebSocket;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Mute.Moe.Extensions;
using Mute.Moe.Models;

namespace Mute.Moe.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly DiscordSocketClient _client;

        public AccountController(DiscordSocketClient client)
        {
            _client = client;
        }

        [HttpGet("LoginDiscord")]
        public IActionResult LoginDiscord(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "Discord");
        }

        [Route("Login")]
        public IActionResult AccountLogin([FromQuery] string returnUrl = "/")
        {
            return Redirect($"/?returnUrl={Uri.EscapeUriString(returnUrl)}");
        }

        [Route("SignOut")]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Route("AccessDenied")]
        public async Task<IActionResult> AccessDenied([FromQuery] string returnUrl = "/")
        {
            return Redirect($"/Error/401?returnUrl={Uri.EscapeUriString(returnUrl)}");
        }

        [HttpGet("Avatar")]
        public async Task<IActionResult> AccountAvatar()
        {
            //Get and the discord ID. If it's missing return a default image
            var id = User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier && c.Issuer == "Discord")?.Value;
            if (id == null)
                return Redirect("images/Discord-Logo-Color.png");

            //Get the avatar value, if it's blank return the default avatar
            var avatar = User.FindFirstValue("urn:discord:avatar");
            if (string.IsNullOrWhiteSpace(avatar))
                return Redirect("images/Discord-Logo-Color.png");

            return Redirect($"https://cdn.discordapp.com/avatars/{id}/{avatar}.jpeg");
        }

        [HttpGet("Guilds")]
        public async Task<GuildInfo[]> AccountGuilds()
        {
            var du = User.TryGetDiscordUser(_client);
            if (du == null)
                return Array.Empty<GuildInfo>();

            return du.MutualGuilds.Select(g => new GuildInfo(g)).ToArray();
        }
    }
}