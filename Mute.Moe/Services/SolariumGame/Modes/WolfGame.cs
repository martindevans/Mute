using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grpc.Core;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.Utilities;
using Solarium;

namespace Mute.Moe.Services.SolariumGame.Modes
{
    public class WolfGame
        : BaseGameModeEventHandler
    {
        private bool _night;
        private bool _started;

        private IPlayer[] _players;

        public WolfGame(IGame game, DiscordSocketClient client, ISolarium solarium, IMessageChannel gameTextChannel)
            : base(game, client, solarium, gameTextChannel)
        {
        }

        public override async Task Start(string address, ChannelCredentials credentials)
        {
            await UpdateStatus();

            await GameTextChannel.SendMessageAsync(Game.GameId);

            await base.Start(address, credentials);
        }

        [ItemNotNull] private async Task<GameStatusResponse> UpdateStatus()
        {
            var status = await Solarium.GameStatus(Game);

            _night = status.TheWolfGame.IsNight;
            _started = status.TheWolfGame.IsStarted;
            _players = await Solarium.Players(Game).ToArray();

            return status;
        }

        protected override async Task HandleUserMessage(IUser user, IMessageChannel context, string message)
        {
            if (message.Contains("lynch", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!_started)
                    await context.SendMessageAsync("The game has not started yet");
                else if (_night)
                    await context.SendMessageAsync("Cannot vote to lynch a player, it is night!");
                else
                    await VoteAction(user, context, message);
            }
            else if (message.Contains("murder", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!_started)
                    await context.SendMessageAsync("The game has not started yet");
                else if (!_night)
                    await context.SendMessageAsync("Cannot vote to murder a player, it is not night!");
                else
                    await VoteAction(user, context, message);
            }
            else if (message.Contains("start", StringComparison.InvariantCultureIgnoreCase))
            {
                await Solarium.DoAction(a => {
                    a.TheWolfGame = new TheWolfGameAction {
                        StartVote = new TheWolfGameAction.Types.VoteStart(),
                    };
                }, user.Id, Game);
            }
            else if (message.Contains("help", StringComparison.InvariantCultureIgnoreCase))
            {
                await context.SendMessageAsync("`lynch @person` to vote for a lynch target");
                await context.SendMessageAsync("`murder @person` to vote for a murder target (for wolves only)");
                await context.SendMessageAsync("`start` to vote to start the game");
            }

            await UpdateStatus();
        }

        [NotNull,] private async Task VoteAction([NotNull] IUser user, [NotNull] IMessageChannel context, [NotNull] string message)
        {
            var player = await FindPlayer(message);
            if (player == null)
            {
                await context.SendMessageAsync("Sorry, not sure which user you mean.");
                return;
            }

            await Solarium.DoAction(a => {
                a.TheWolfGame = new TheWolfGameAction {
                    Vote = new TheWolfGameAction.Types.VoteMurder {
                        PlayerId = player.SolariumId
                    }
                };
            }, user.Id, Game);
        }


        protected override async Task HandleEvent(GameUpdateResponse @event)
        {
            foreach (var evt in @event.Events)
            {
                if (evt.TheWolfGame.NewPlayer != null)
                    await HandleJoinEvent(evt.TheWolfGame.NewPlayer);
                else if (evt.TheWolfGame.PlayerDied != null)
                    await HandleDeathEvent(evt.TheWolfGame.PlayerDied);
                else if (evt.TheWolfGame.Transisition != null)
                    await HandleTransitionEvent(evt.TheWolfGame.Transisition);
                else if (evt.TheWolfGame.GameStart != null)
                    await HandleStartEvent(evt.TheWolfGame.GameStart);
                else if (evt.TheWolfGame.VillageVictory != null)
                    await GameTextChannel.SendMessageAsync("Villagers win!");
                else if (evt.TheWolfGame.WolfVictory != null)
                    await GameTextChannel.SendMessageAsync("Werewolves win!");
                else
                    await GameTextChannel.SendMessageAsync($"UnknownGameEvent! `[{evt.Name}]`: \"{evt.Desc}\"");
            }

            var status = await UpdateStatus();

            // Mute dead players in this channel
            var gc = Client.GetGuild(Game.GuildId)?.GetTextChannel(Game.ChannelId);
            if (gc != null)
            {
                foreach (var player in status.TheWolfGame.Players)
                {
                    if (!player.IsAlive)
                    {
                        var p = _players.SingleOrDefault(x => x.SolariumId == player.ID);
                        if (p != null)
                            await gc.AddPermissionOverwriteAsync(p.User, new OverwritePermissions(sendMessages: PermValue.Deny));
                    }
                }
            }
        }

        private async Task HandleStartEvent(TheWolfGameEvent.Types.GameStarted _)
        {
            // Fetch latest status
            await UpdateStatus();

            // Build list of players by alignment
            var villagers = new List<IPlayer>();
            var wolves = new List<(IUser, string)>();
            foreach (var player in _players)
            {
                var status = await Solarium.GameStatus(Game, player.SolariumId);

                // If any player in the set is a wolf then the set contains all the wolves
                if (status.TheWolfGame.Players.Any(r => r.Role == TheWolfGamePlayer.Types.PlayerRole.Werewolf))
                    wolves.AddRange(status.TheWolfGame.Players.Where(p => p.Role == TheWolfGamePlayer.Types.PlayerRole.Werewolf).Select(p => (_players.SingleOrDefault(u => u.SolariumId == p.ID)?.User, p.Name)));
                else
                    villagers.Add(player);
            }


            var name = $"{Client.GetGuild(Game.GuildId).Name}/{Game.Name}";

            // Tell villagers who they are
            foreach (var player in villagers)
                if (player != null)
                    await player.User.SendMessageAsync($"Your role in the Wolf Game ({name}) is an innocent villager! During the day you should vote for who you believe to be a werewolf, during the night you should cower in terror.");

            // Tell werewolves who they are
            var wv = string.Join(", ", wolves.Select(w => w.Item2));
            foreach (var (p, _) in wolves)
                await p.SendMessageAsync($"Your role in the Wolf Game ({name}) is a vicious werewolf! The werewolves are: {wv}");

            await GameTextChannel.SendMessageAsync("The game has started:");
            await PrintPlayerStatuses();

            _started = true;
        }

        private async Task HandleTransitionEvent([NotNull] TheWolfGameEvent.Types.TimeTransistion transisition)
        {
            _night = transisition.IsNight;

            if (_night)
                await GameTextChannel.SendMessageAsync("Night has fallen.");
            else
                await GameTextChannel.SendMessageAsync("The sun dawns on a new day.");

            if (!_night)
                await PrintPlayerStatuses();
        }

        private async Task HandleJoinEvent([NotNull] TheWolfGameEvent.Types.PlayerJoined joined)
        {
            if (joined == null)
                throw new ArgumentNullException(nameof(joined));
            // No message printed. Local joins get a different event, remote joins are not shown until the game starts
        }

        private async Task HandleDeathEvent([NotNull] TheWolfGameEvent.Types.PlayerDeath death)
        {
            await GameTextChannel.SendMessageAsync($"{death.PlayerName} has died");

            var gc = GameTextChannel as SocketTextChannel ?? Client.GetGuild(Game.GuildId)?.GetTextChannel(Game.ChannelId);
            if (gc != null)
            {
                var p = _players.SingleOrDefault(x => x.SolariumId == death.PlayerID);
                if (p != null)
                    await gc.AddPermissionOverwriteAsync(p.User, new OverwritePermissions(sendMessages: PermValue.Deny));
            }
        }


        [NotNull, ItemCanBeNull] private async Task<IPlayer> FindPlayer([NotNull] string message)
        {
            var mentions = message.FindUserMentions().ToArray();

            if (!mentions.Any())
                return null;

            if (mentions.Length > 1)
                return null;

            var user = Client.GetGuild(Game.GuildId)?.GetUser(mentions.Single());
            if (user == null)
                return null;

            return _players.SingleOrDefault(p => p.User.Id == user.Id);
        }

        private async Task PrintPlayerStatuses()
        {
            var sb = new StringBuilder();

            var rng = new Random();
            var status = await UpdateStatus();
            foreach (var (name, alive) in status.TheWolfGame.Players.Select(p => (p.Name, p.IsAlive)).OrderBy(a => a.Name))
            {
                if (alive)
                    sb.AppendLine($"- {name}");
                else
                    sb.AppendLine($"- ~~{name}~~ { new[] { EmojiLookup.Skull, EmojiLookup.SkullAndCrossbones, EmojiLookup.Coffin }.Random(rng) }");
            }

            await GameTextChannel.SendMessageAsync(sb.ToString());
        }
    }
}
