using Discord;
using Discord.Commands;
using Mute.Moe.Services.Information.Steam;
using System.Linq;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules
{
    [Group("steam")]
    public class Steam
        : BaseModule
    {
        private readonly ISteamInfo _steamApi;
        private readonly ISteamIdStorage _ids;

        public Steam(ISteamInfo steamApi, ISteamIdStorage ids)
        {
            _steamApi = steamApi;
            _ids = ids;
        }

        [Command("setid"), Summary("I will remember your steam ID")]
        public async Task SetSteamId(ulong id)
        {
            await _ids.Set(Context.User.Id, id);
        }

        [Command("recent")]
        public async Task GetRecentGames(IUser? user = null)
        {
            user ??= Context.User;

            var steamId = await _ids.Get(user.Id);
            if (!steamId.HasValue)
            {
                await TypingReplyAsync("I'm sorry, I don't know your steam ID. Please use `setid` to set it");
                return;
            }

            var games = (await _steamApi.GetRecentlyPlayedGames(steamId.Value)).ToArray();

            await DisplayItemList(games, () => "No games played recently", ls => $"{ls.Count} games played recently", (a, i) => $"{i}. {a.Name}");
        }
    }
}
