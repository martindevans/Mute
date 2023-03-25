﻿using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.OtherBots;

[UsedImplicitly]
[Hidden]
public class Hugot
    : BaseModule
{
    private const double ReactionChance = 0.05;
    private const double ReplyChance = 0.01;

    private readonly Random _random;

    private readonly string[] _responses = {
        "I see how it is",
        "What does he have that I don't?",
        "Curse your sudden, but inevitable, betrayal!",
        "Hmph",
    };

    private readonly string[] _reactions = {
        EmojiLookup.Smirk,
        EmojiLookup.Expressionless,
        EmojiLookup.RollingEyes,
        EmojiLookup.Worried,
    };

    public Hugot(Random random)
    {
        _random = random;
    }

    [Command("hugot"), Hidden]
    public async Task DoNothing(params string[] _)
    {
        if (_random.NextDouble() < ReactionChance)
            await Context.Message.AddReactionAsync(new Emoji(_reactions[_random.Next(_responses.Length)]));
        else if (_random.NextDouble() <= ReplyChance)
            await TypingReplyAsync(_responses[_random.Next(_responses.Length)]);
    }
}