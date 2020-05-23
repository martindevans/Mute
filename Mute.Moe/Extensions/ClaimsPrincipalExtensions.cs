using System.Security.Claims;
using System.Threading.Tasks;
using Discord.WebSocket;


namespace Mute.Moe.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static SocketUser? TryGetDiscordUser(this ClaimsPrincipal user, DiscordSocketClient client)
        {
            var idClaim = user.FindFirst(c => c.Type == ClaimTypes.NameIdentifier && c.Issuer == "Discord");
            if (idClaim == null)
                return null;

            if (!ulong.TryParse(idClaim.Value ?? "", out var id))
                return null;

            return client.GetUser(id);
        }

        public static async Task<bool> IsBotOwner(this ClaimsPrincipal user, DiscordSocketClient client)
        {
            var discordUser = user.TryGetDiscordUser(client);
            if (discordUser == null)
                return false;

            var info = await client.GetApplicationInfoAsync();
            if (info.Owner == null)
                return false;

            return info.Owner.Id == discordUser.Id;
        }

        public static async Task<bool> IsInBotGuild(this ClaimsPrincipal user, DiscordSocketClient client)
        {
            var discordUser = user.TryGetDiscordUser(client);
            if (discordUser == null)
                return false;

            return discordUser.MutualGuilds.Count > 0;
        }
    }
}
