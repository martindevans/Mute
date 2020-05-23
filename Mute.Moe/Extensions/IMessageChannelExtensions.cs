using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MoreLinq;

namespace Mute.Moe.Extensions
{
    public static class IMessageChannelExtensions
    {
        private const float WordsPerMinute = 360;
        private const float CharactersPerSecond = 12;

        private static readonly TimeSpan SoftMaxDelay = TimeSpan.FromSeconds(2.0);

        public static async Task<IUserMessage> TypingReplyAsync(this IMessageChannel channel,  string message, bool isTTS = false, Embed? embed = null, RequestOptions? options = null)
        {
            using (channel.EnterTypingState())
            {
                await Task.Delay(Delay(message));
                return await channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            }
        }

        private static TimeSpan Delay( string message)
        {
            var wordTime = message.Count(c => c == ' ') / WordsPerMinute;
            var symbTime = (message.Length - message.Count(char.IsLetter)) / (CharactersPerSecond * 180);

            var delay = TimeSpan.FromMinutes(wordTime + symbTime);
            if (delay <= SoftMaxDelay)
                return delay;

            //Beyond the soft max only increase the delay very slowly
            return SoftMaxDelay + TimeSpan.FromSeconds(Math.Pow((delay - SoftMaxDelay).TotalSeconds, 0.25f));
        }



        public static async Task SendLongMessageAsync( this IMessageChannel channel, string message)
        {
            var strings = message.Batch(1900).Select(a => new string(a.ToArray())).ToArray();

            foreach (var item in strings)
            {
                await channel.SendMessageAsync(item);
                await Task.Delay(200);
            }
        }
    }
}
