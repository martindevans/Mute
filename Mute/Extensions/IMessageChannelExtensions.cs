using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class IMessageChannelExtensions
    {
        private const float WordsPerMinute = 360;
        private const float CharactersPerSecond = 12;

        public static async Task<IUserMessage> TypingReplyAsync([NotNull] this IMessageChannel channel, [NotNull] string message, bool isTTS = false, [CanBeNull] Embed embed = null, [CanBeNull] RequestOptions options = null)
        {

            using (channel.EnterTypingState())
            {
                await Task.Delay(Delay(message));
                return await channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            }
        }

        private static TimeSpan Delay([NotNull] string message)
        {
            var wordTime = message.Count(c => c == ' ') / WordsPerMinute;
            var symbTime = (message.Length - message.Count(char.IsLetter)) / (CharactersPerSecond * 180);

            return TimeSpan.FromMinutes(wordTime + symbTime);
        }
    }
}
