using System.Security.Claims;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace Mute.Moe.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static SocketUser TryGetDiscordUser([NotNull] this ClaimsPrincipal user, DiscordSocketClient client)
        {
            var idClaim = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier && c.Issuer == "Discord");
            if (idClaim == null)
                return null;

            if (!ulong.TryParse(idClaim.Value ?? "", out var id))
                return null;

            return client.GetUser(id);
        }
    }
}
