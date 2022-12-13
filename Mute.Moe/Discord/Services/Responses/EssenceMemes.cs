using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Services.Responses;

public class EssenceMemes
    : IResponse
{
    public double BaseChance => 0.15;
    public double MentionedChance => 1;

    private readonly Random _random;

    private readonly HashSet<string> _triggerWords = new() {
        "essence",
    };

    public EssenceMemes(Random random)
    {
        _random = random;
    }

    public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
    {
        var rgx = new Regex("[^a-zA-Z0-9 -]");
        var msg = rgx.Replace(context.Message.Content, "");

        var words = msg.ToLowerInvariant().Split(' ');
        if (_triggerWords.Overlaps(words))
        {
            if (_random.NextDouble() < 0.5f)
                return new TerminalConversation(Text());
            return new TerminalConversation(null, Emotes());
        }

        return null;
    }

    #region response generator
    private static IEmote[] Emotes()
    {
        return new IEmote[] { new Emoji(EmojiLookup.CrystalBall) };
    }

    private string Text()
    {
        var gif = _gifs.Random(_random);
        return $"{gif}";
    }
    #endregion

    #region data
    private readonly IReadOnlyList<string> _gifs = new[] {
        "https://i.imgur.com/iJS2UgR.gif",
        "https://c.tenor.com/UellOL75INkAAAAM/dark-crystal-chamberlain-skeksis.gif",
        "https://64.media.tumblr.com/4a5ea64eff44a62e9987e4c9a3923756/bab8d2162e3b9037-1a/s540x810/f9cb0b2d43634ae5f24f9b7354ef44fc5a23fdf3.gif"
    };
    #endregion
}