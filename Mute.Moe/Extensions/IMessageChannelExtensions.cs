using Discord;
using Mute.Moe.Discord.Services.Responses;
using System.Globalization;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IMessageChannel"/>
/// </summary>
public static class IMessageChannelExtensions
{
    private const float WordsPerMinute = 360;
    private const float CharactersPerSecond = 12;

    private static readonly TimeSpan SoftMaxDelay = TimeSpan.FromSeconds(2.0);

    /// <param name="this"></param>
    extension(IMessageChannel @this)
    {
        /// <summary>
        /// Show the typing state, and then send a message after some delay. As if it was typed.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isTTS"></param>
        /// <param name="embed"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<IUserMessage> TypingReplyAsync(string message, bool isTTS = false, Embed? embed = null, RequestOptions? options = null)
        {
            using (@this.EnterTypingState())
            {
                await Task.Delay(Delay(message));
                return await @this.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
            }

            static TimeSpan Delay(string message)
            {
                var wordTime = message.Count(c => c == ' ') / WordsPerMinute;
                var symbTime = (message.Length - message.Count(char.IsLetter)) / (CharactersPerSecond * 180);

                var delay = TimeSpan.FromMinutes(wordTime + symbTime);
                if (delay <= SoftMaxDelay)
                    return delay;

                // Beyond the soft max only increase the delay very slowly
                return SoftMaxDelay + TimeSpan.FromSeconds(Math.Pow((delay - SoftMaxDelay).TotalSeconds, 0.25f));
            }
        }

        /// <summary>
        /// Send a long message in a channel, splitting it up to avoid the length limit
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public async Task<IReadOnlyList<IUserMessage>> SendLongMessageAsync(string content)
        {
            var messages = new List<IUserMessage>();

            if (content.Length < 2000)
            {
                messages.Add(await @this.SendMessageAsync(content));
                return messages;
            }

            var strings = SplitOnSpaces(content).ToList();
            foreach (var item in strings)
            {
                messages.Add(await @this.SendMessageAsync(item));
                await Task.Delay(200);
            }

            return messages;

            static IEnumerable<string> SplitOnSpaces(string content, int maxLength = 1950)
            {
                if (content.Length <= maxLength)
                {
                    yield return content;
                    yield break;
                }

                var remainder = content.AsMemory();
                while (remainder.Length > 0)
                {
                    if (remainder.Length <= maxLength)
                    {
                        yield return new string(remainder.Span);
                        yield break;
                    }

                    var idx = remainder[..maxLength].Span.LastIndexOf(' ');
                    if (idx < 0)
                        idx = maxLength;

                    var slice = remainder[..idx];
                    remainder = remainder[idx..];

                    yield return new string(slice.Span);
                }
            }
        }

        /// <summary>
        /// Get an embed with info about the conversation state in this channel
        /// </summary>
        /// <returns></returns>
        public async Task<EmbedBuilder> GetConversationStateEmbed(ConversationalResponseService conversations)
        {
            // Get the conversation
            var conversation = await conversations.GetConversation(@this);
            var stats = conversation.ContextStatistics;
            
            var embed = new EmbedBuilder()
                       .WithTitle($"Conversation for {conversation.Channel.Name}")
                       .WithTimestamp(conversation.LastUpdated);

            var fields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder().WithIsInline(true).WithName("Processing State").WithValue(conversation.State.ToString()),
                new EmbedFieldBuilder().WithIsInline(true).WithName("Event Queue").WithValue(conversation.QueueCount.ToString()),
            };

            double? usage = null;
            if (stats.Usage?.TotalTokenCount is long total)
            {
                fields.Add(
                    new EmbedFieldBuilder()
                       .WithIsInline(true)
                       .WithName("Total Tokens")
                       .WithValue(total)
                );

                usage = (double)total / stats.ContextSize;
                if (usage.HasValue)
                {
                    fields.Add(
                        new EmbedFieldBuilder()
                           .WithIsInline(true)
                           .WithName("Usage")
                           .WithValue(usage.Value.ToString("P1", CultureInfo.InvariantCulture))
                    );
                }
            }

            if (stats.Messages is int messageCount)
            {
                fields.Add(
                    new EmbedFieldBuilder()
                       .WithIsInline(true)
                       .WithName("Total Messages")
                       .WithValue(messageCount)
                );
            }

            embed.WithFields(fields);
            
            // Color ramp based on context usage
            if (usage.HasValue)
            {
                (float r, float g, float b) color = usage.Value switch
                {
                    < 0.15f => (0.0f, 1.0f, 0.0f), // green
                    < 0.22f => (0.0f, 0.8f, 0.4f), // green-cyan
                    < 0.30f => (0.0f, 0.5f, 1.0f), // blue
                    < 0.38f => (0.4f, 0.7f, 1.0f), // light blue
                    < 0.46f => (1.0f, 1.0f, 0.0f), // yellow
                    < 0.54f => (1.0f, 0.8f, 0.0f), // yellow-orange
                    < 0.62f => (1.0f, 0.5f, 0.0f), // orange
                    < 0.75f => (1.0f, 0.25f, 0.0f), // deep orange
                    _ => (1.0f, 0.0f, 0.0f), // red
                };
                embed.WithColor(color.r, color.g, color.b);
            }

            return embed;
        }
    }
}