using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class UserInfo
        : BaseModule, IKeyProvider
    {
        private readonly DiscordSocketClient _client;

        public UserInfo(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("userid"), Summary("I will type out the ID of the specified user")]
        public async Task UserId(IUser user = null)
        {
            user = user ?? Context.User;

            await TypingReplyAsync($"User ID for {user.Username} is `{user.Id}`");
        }

        [Command("whois"), Summary("I will print out a summary of information about the given user")]
        public async Task Whois([CanBeNull] IUser user = null)
        {
            await TypingReplyAsync(GetUserInfo(user ?? Context.User));
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

        private static string GetUserInfo([NotNull] IUser user)
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
