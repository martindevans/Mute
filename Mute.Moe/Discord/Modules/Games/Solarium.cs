using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.SolariumGame;
using Solarium;

namespace Mute.Moe.Discord.Modules.Games
{
    [HelpGroup("games")]
    [Group("solarium")]
    public class Solarium
        : BaseModule
    {
        private readonly ISolarium _solarium;

        public Solarium(ISolarium solarium)
        {
            _solarium = solarium;
        }

        [RequireOwner]
        [Command("enable")]
        public async Task EnableSolarium(string name)
        {
            var category = Context.Guild.CategoryChannels.SingleOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (category == null)
            {
                await TypingReplyAsync("Cannot find a channel category by that name");
                return;
            }

            await _solarium.SetCategory(Context.Guild.Id, category.Id);
            await TypingReplyAsync($"New games of solarium will be created in the `{category.Name}` category");
        }

        [RequireOwner]
        [Command("new")]
        public async Task NewGame(NewGameRequest.Types.GameMode mode = NewGameRequest.Types.GameMode.Thewolfgame, NewGameRequest.Types.DifficultyLevel difficulty = NewGameRequest.Types.DifficultyLevel.Normal)
        {
            var category = await _solarium.GetCategory(Context.Guild.Id);
            if (category == null)
            {
                await TypingReplyAsync("This guild doesn't have Solarium games enabled");
                return;
            }

            var game = await _solarium.CreateGame(Context.Guild.Id, mode, difficulty);
            if (game == null)
            {
                await TypingReplyAsync("I couldn't create a new game, sorry");
            }
        }

        [RequireOwner]
        [Command("delete")]
        public async Task StopGame()
        {
            var category = await _solarium.GetCategory(Context.Guild.Id);
            if (category == null)
            {
                await TypingReplyAsync("This guild doesn't have Solarium games enabled");
                return;
            }

            var game = await _solarium.GetGames(Context.Guild.Id, Context.Channel.Id).SingleOrDefault();
            if (game == null)
            {
                await TypingReplyAsync("There are no solarium games associated with this channel");
                return;
            }

            //await TypingReplyAsync("Type the name of this channel to **destroy this Solarium game**? This cannot be undone!");
            //var confirm = await NextMessageAsync(true, true, TimeSpan.FromSeconds(45));
            //if (confirm == null)
            //{
            //    await TypingReplyAsync("You did not confirm destroying the game in time. Cancelled.");
            //    return;
            //}

            //if (confirm.Content.Equals(Context.Channel.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                //await TypingReplyAsync("This game will be destroyed. Deletion will be cancelled if anyone speaks in the channel in the next 10 seconds");
                //var interrupt = await NextMessageAsync(false, true, TimeSpan.FromSeconds(10));

                //if (interrupt != null)
                //    await TypingReplyAsync("Deletion cancelled");
                //else
                    await _solarium.DestroyGame(game);
            }
        }

        [Command("join")]
        public async Task JoinGame()
        {
            var game = await _solarium.Join(Context.Guild.Id, Context.Channel.Id, Context.User.Id);

            if (game == null)
                await TypingReplyAsync("Cannot join Solarium game from here");
            else
                await TypingReplyAsync($"{Context.User.Mention} has joined the game");
        }

        [Command("action")]
        public async Task Action([NotNull] string id, [Remainder, NotNull] string action)
        {
            _solarium.InjectGameEvent(id, Context.Message.Author, Context.Channel, action);
        }
    }
}
