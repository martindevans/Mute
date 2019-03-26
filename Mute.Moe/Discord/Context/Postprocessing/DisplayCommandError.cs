using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Modules;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public class DisplayCommandError
        : IUnsuccessfulCommandPostprocessor
    {
        private readonly CommandService _commands;

        public DisplayCommandError(CommandService commands)
        {
            _commands = commands;
        }

        public async Task Process(MuteCommandContext context, [NotNull] IResult result)
        {
            if (result.Error == CommandError.UnknownCommand)
            {
                var input = context.Message.Content ?? "";
                var spaceIndex = input.IndexOf(' ');
                var inputCmd = input;
                if (spaceIndex != -1)
                    inputCmd = input.Substring(0, spaceIndex);

                var closest = _commands.Commands.Select(c => new { c, d = c.Aliases.Append(c.Name).Min(n => n.Levenshtein(inputCmd)) }).Aggregate((a, b) => a.d < b.d ? a : b);
                if (closest == null)
                {
                    await context.Channel.SendMessageAsync("Unknown command");
                    return;
                }

                await context.Channel.TypingReplyAsync("I don't know that command, did you mean:", embed: Commands.FormatCommandDetails(closest.c, context.Client.CurrentUser).Build());
            }
            else
            {
                if (result.ErrorReason != null)
                    await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}
