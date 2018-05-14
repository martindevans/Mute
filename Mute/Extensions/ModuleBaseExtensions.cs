using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace Mute.Extensions
{
    public static class ModuleBaseExtensions
    {
        const float WordsPerMinute = 360;
        private const float CharactersPerSecond = 12;

        public static async Task<IUserMessage> TypingReplyAsync(this ModuleBase module, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            using (module.Context.Channel.EnterTypingState())
            {
                await Task.Delay(Delay(message));
                return await module.Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            }
        }

        public static async Task<IUserMessage> TypingReplyAsync(this InteractiveBase module, string message, bool isTTS = false, Embed embed = null, RequestOptions options = null)
        {
            using (module.Context.Channel.EnterTypingState())
            {
                await Task.Delay(Delay(message));
                return await module.Context.Channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            }
        }

        private static TimeSpan Delay(string message)
        {
            var wordTime = message.Count(c => c == ' ') / WordsPerMinute;
            var symbTime = (message.Length - message.Count(char.IsLetter)) / (CharactersPerSecond * 180);

            return TimeSpan.FromMinutes(wordTime + symbTime);
        }
    }
}
