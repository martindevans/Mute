using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Mute.Moe.Services.Information.Steam;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Responses.Eliza;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using MoreLinq;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Discord.Modules
{
    [Group("steam")]
    public class Steam
        : BaseModule, IKeyProvider
    {
        private readonly ISteamInfo _steamApi;
        private readonly ISteamIdStorage _ids;
        private readonly ISteamLightweightAppInfoStorage _steamApiLight;
        private readonly Configuration _config;
        private readonly IUserService _users;

        public Steam(ISteamInfo steamApi, ISteamIdStorage ids, ISteamLightweightAppInfoStorage steamApiLight, Configuration config, IUserService users)
        {
            _steamApi = steamApi;
            _ids = ids;
            _steamApiLight = steamApiLight;
            _config = config;
            _users = users;
        }

        [Command("setid"), Summary("I will remember your steam ID")]
        public async Task SetSteamId(ulong id)
        {
            await _ids.Set(Context.User.Id, id);
        }

        [Command("getid"), Summary("I will print you Steam ID (if I know it)")]
        public async Task GetSteamId(IUser? user = null)
        {
            user ??= Context.User;

            var id = await _ids.Get(user.Id);
            if (id == null)
            {
                if (user.Id == Context.User.Id)
                    await TypingReplyAsync("I don't know your steam ID. Please check https://steamid.io/ and then use `!steam setid id` to save your ID for the future.");
                else
                    await TypingReplyAsync($"I don't know the steam ID of {_users.Name(user)}");
            }
            else
                await TypingReplyAsync($"Your steam ID is `{id}`");
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

            var played = await _steamApi.GetRecentlyPlayedGames(steamId.Value);
            if (played == null)
            {
                await TypingReplyAsync("I'm sorry, I can't find your recently played games right now");
                return;
            }

            var games = played.ToArray();

            await DisplayItemList(games, () => "No games played recently", ls => $"{ls.Count} games played recently:", (a, i) => $"{i}. {a.Name}");
        }

        [Command("intersection")]
        [ThinkingReply]
        public async Task SteamGamesIntersection([Remainder] string message)
        {
            // First, get the SteamIDs of everyone involved
            var mentions = Context.Message.MentionedUsers;
            var ids = await mentions.ToAsyncEnumerable().SelectAwait(async user => (user, await _ids.Get(user.Id))).ToArrayAsync();
            var unknown = ids.Where(a => a.Item2 == null).Select(a => _users.Name(a.user, true)).ToArray();
            if (unknown.Length > 0)
            {
                await TypingReplyAsync($"I don't know the SteamIDs of {unknown.Humanize()}, please get your IDs with https://steamid.io/ and then use `{_config.PrefixCharacter}steam setid id` to save it");
                return;
            }

            // Get the games of the selected users
            var games = await (
                from user in ids
                where user.Item2 != null
                let steamid = user.Item2!.Value
                let owned = _steamApi.GetOwnedGames(steamid)
                select (user, owned)
            ).ToAsyncEnumerable().SelectAwait(async a => (a.user, await a.owned)).ToArrayAsync();

            // Find the common elements in all libraries
            var common = new HashSet<uint>();
            var first = true;
            foreach (var (_, gs) in games)
            {
                if (gs == null)
                    continue;

                if (first)
                    common.UnionWith(gs.Select(a => a.AppId));
                else
                    common.IntersectWith(gs.Select(a => a.AppId));
                first = false;
            }

            // Early out if there are no common elements
            if (common.Count == 0)
            {
                await TypingReplyAsync("You don't have any common games in your steam libraries");
                return;
            }

            await TypingReplyAsync($"Found {common.Count} common games");

            // Get store info about each game
            var storeInfos = common
                 .Shuffle()
                 .ToAsyncEnumerable()
                 .SelectAwait(async a => await _steamApiLight.Get(a))
                 .Where(a => a != null)
                 .Select(a => a!)
                 .ToEnumerable()
                 .Batch(10)
                 .Select(b => string.Join("\n", b.Select(a => a.Name)));

            await DisplayLazyPaginatedReply("Steam Games", storeInfos);
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                // ReSharper disable once LocalFunctionHidesMethod
                async Task<string> GetSteamId(ICommandContext c)
                {
                    var id = await _ids.Get(c.User.Id);
                    if (id == null)
                        return "I don't know your steam ID. Please check https://steamid.io/ and then use `!steam setid id` to save your ID for the future.";

                    return $"Your steam ID is `{id}`";
                }

                yield return new Key("steam",
                    new Decomposition("*what*my*steam*id*", (c, s) => GetSteamId(c)!),
                    new Decomposition("*what*my*steamid*", (c, s) => GetSteamId(c)!),
                    new Decomposition("*tell*my*steam*id*", (c, s) => GetSteamId(c)!),
                    new Decomposition("*tell*my*steamid*", (c, s) => GetSteamId(c)!)
                );
            }
        }
    }
}
