using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IMessageChannel"/>
/// </summary>
public static class IMessageChannelExtensions
{
    private const float WordsPerMinute = 360;
    private const float CharactersPerSecond = 12;

    private static readonly TimeSpan SoftMaxDelay = TimeSpan.FromSeconds(2.0);

    /// <summary>
    /// Show the typing state, and then send a message after some delay. As if it was typed.
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="message"></param>
    /// <param name="isTTS"></param>
    /// <param name="embed"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static async Task<IUserMessage> TypingReplyAsync(this IMessageChannel channel, string message, bool isTTS = false, Embed? embed = null, RequestOptions? options = null)
    {
        using (channel.EnterTypingState())
        {
            await Task.Delay(Delay(message));
            return await channel.SendMessageAsync(message, isTTS, embed, options).ConfigureAwait(false);
        }
    }

    private static TimeSpan Delay(string message)
    {
        var wordTime = message.Count(c => c == ' ') / WordsPerMinute;
        var symbTime = (message.Length - message.Count(char.IsLetter)) / (CharactersPerSecond * 180);

        var delay = TimeSpan.FromMinutes(wordTime + symbTime);
        if (delay <= SoftMaxDelay)
            return delay;

        //Beyond the soft max only increase the delay very slowly
        return SoftMaxDelay + TimeSpan.FromSeconds(Math.Pow((delay - SoftMaxDelay).TotalSeconds, 0.25f));
    }

    /// <summary>
    /// Send a long message in a channel, splitting it up to avoid the length limit
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public static async Task<IReadOnlyList<IUserMessage>> SendLongMessageAsync(this IMessageChannel channel, string content)
    {
        var messages = new List<IUserMessage>();

        if (content.Length < 2000)
        {
            messages.Add(await channel.SendMessageAsync(content));
            return messages;
        }

        var strings = SplitOnSpaces(content).ToList();
        foreach (var item in strings)
        {
            messages.Add(await channel.SendMessageAsync(item));
            await Task.Delay(200);
        }

        return messages;
    }

    private static IEnumerable<string> SplitOnSpaces(string content, int maxLength = 1950)
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

            var idx = content[..maxLength].LastIndexOf(' ');
            if (idx < 0)
                idx = maxLength;
            
            var slice = remainder[..idx];
            remainder = remainder[idx..];

            yield return new string(slice.Span);
        }
    }
}