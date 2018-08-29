using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Extensions;

namespace Mute.Modules
{
    public class UserInfo
        : ModuleBase
    {
        private readonly DiscordSocketClient _client;

        public UserInfo(DiscordSocketClient client)
        {
            _client = client;
        }

        [Command("introduce"), Summary("I will introduce myself")]
        [RequireOwner]
        public async Task Intro(string _)
        {
            await this.TypingReplyAsync("Hello everyone, I'm *Mute");
            await this.TypingReplyAsync("The * means I'm an AI, I hope you won't hold that against me though");
            await this.TypingReplyAsync("I don't really know what I'm doing here yet...");
        }

        [Command("userid"), Summary("I will type out the ID of the specified user")]
        public async Task UserId(IUser user = null)
        {
            user = user ?? Context.User;

            await this.TypingReplyAsync($"User ID for {Context.User.Username} is '{user.Id}'");
        }

        [Command("whois"), Summary("I will print out a summary of information about the given user")]
        public async Task Whois(IUser user = null)
        {
            user = user ?? Context.User;

            var str = $"{user.Username}";

            var gu = user as IGuildUser;

            if (gu?.Nickname != null)
                str += $" AKA {gu.Nickname}";

            if (user.IsBot && !user.Username.StartsWith('*'))
                str += " is a bot";

            if (gu?.Activity != null)
                str += $" ({gu.Activity.Type} {gu.Activity.Name})";

            await this.TypingReplyAsync(str);
        }
    }
}
