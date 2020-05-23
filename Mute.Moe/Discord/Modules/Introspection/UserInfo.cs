using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Mute.Moe.Discord.Services.Responses.Eliza;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;

namespace Mute.Moe.Discord.Modules.Introspection
{
    public class UserInfo
        : BaseModule, IKeyProvider
    {
        private readonly DiscordSocketClient _client;
        private readonly HttpClient _http;

        public UserInfo(DiscordSocketClient client, IHttpClientFactory http)
        {
            _client = client;
            _http = http.CreateClient();
        }

        [Command("userid"), Summary("I will type out the ID of the specified user")]
        public async Task UserId(IUser? user = null)
        {
            user ??= Context.User;

            await TypingReplyAsync($"User ID for {user.Username} is `{user.Id}`");
        }

        [Command("whois"), Summary("I will print out a summary of information about the given user")]
        public async Task Whois(IUser? user = null)
        {
            await TypingReplyAsync(GetUserInfo(user ?? Context.User));
        }

        [Command("avatar"), Summary("I will show the avatar for a user")]
        public async Task Avatar(IUser? user = null)
        {
            var u = user ?? Context.User;
            var url = u.GetAvatarUrl(ImageFormat.Png, 2048);

            using var resp = await _http.GetAsync(url);
            var m = new MemoryStream();
            await resp.Content.CopyToAsync(m);
            m.Position = 0;

            await Context.Channel.SendFileAsync(m, $"{Name(u)}.png");
        }

        private string GetUserInfo(string userid)
        {
            if (!MentionUtils.TryParseUser(userid, out var id))
                return "I don't know who that is";

            var users = (from g in _client.Guilds
                         let gu = g.GetUser(id)
                         select gu).ToArray();

            if (users.Length == 1)
                return GetUserInfo(users.Single());
            else
                return GetUserInfo(_client.GetUser(id));
        }

        private static string GetUserInfo( IUser user)
        {
            var str = new StringBuilder($"{user.Username}");

            var gu = user as IGuildUser;

            if (gu?.Nickname != null)
                str.Append($" AKA {gu.Nickname}");

            int clause = 0;
            if (user.IsBot && !user.Username.StartsWith('*'))
            {
                str.Append(" is a bot");
                clause++;
            }

            if (gu?.Activity != null)
            {
                str.Append($" is currently {gu.Activity.Type} {gu.Activity.Name}");
                clause++;
            }

            if (gu?.JoinedAt != null)
            {
                if (clause > 0)
                    str.Append(" and");

                var duration = (DateTime.UtcNow - gu.JoinedAt.Value.UtcDateTime).Humanize();

                str.Append($" has been a member for {duration}");
            }

            return str.ToString();
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("who", 10,
                    new Decomposition("who is *", d => GetUserInfo(d[0])),
                    new Decomposition("who * is *", d => GetUserInfo(d[1]))
                );
            }
        }
    }
}
