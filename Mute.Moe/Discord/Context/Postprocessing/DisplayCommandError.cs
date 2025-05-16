﻿using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Discord.Modules.Introspection;

namespace Mute.Moe.Discord.Context.Postprocessing;

public class DisplayCommandError
    : IUnsuccessfulCommandPostprocessor
{
    private readonly CommandService _commands;
    private readonly Random _random;
    private readonly Configuration _config;
    private readonly char _prefix;

    public DisplayCommandError(Configuration config, CommandService commands, Random random)
    {
        _commands = commands;
        _random = random;
        _config = config;
        _prefix = config.PrefixCharacter;
    }

    public uint Order => uint.MaxValue;

    public async Task<bool> Process(MuteCommandContext context,  IResult result)
    {
        if (result.Error == CommandError.UnknownCommand)
        {
            var input = context.Message.Content ?? "";
            var spaceIndex = input.IndexOf(' ');
            var inputCmd = input;
            if (spaceIndex != -1)
                inputCmd = input[..spaceIndex];

            inputCmd = inputCmd.TrimStart(_config.PrefixCharacter);

            // Take a random command from the group of commands which are closest
            var closest = _commands.Commands
                .Select(c => new { c, d = c.Aliases.Append(c.Name).Min(n => n.Levenshtein(inputCmd)) })
                .GroupBy(a => a.d)
                .MinBy(a => a.Key)
                .Random(_random);

            // If we can't find a command, or the one we found has too many differences to be considered, just exit out
            if (closest == null || closest.d >= inputCmd.Length * 0.5)
            {
                await context.Channel.SendMessageAsync("I don't know that command :confused:");
                return false;
            }

            // Suggest a potential matched command
            await context.Channel.TypingReplyAsync("I don't know that command, did you mean:", embed: Help.FormatCommandDetails(context, _prefix, [ closest.c ]).Build());
        }
        else
        {
            if (result.ErrorReason != null)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        return false;
    }
}