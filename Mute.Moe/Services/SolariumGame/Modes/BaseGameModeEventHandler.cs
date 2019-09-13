using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grpc.Core;
using JetBrains.Annotations;
using Solarium;

namespace Mute.Moe.Services.SolariumGame.Modes
{
    public abstract class BaseGameModeEventHandler
    {
        protected IGame Game { get; }
        protected ISolarium Solarium { get; }
        protected IMessageChannel GameTextChannel { get; }
        protected DiscordSocketClient Client { get; }

        protected BaseGameModeEventHandler(IGame game, DiscordSocketClient client, ISolarium solarium, IMessageChannel gameTextChannel)
        {
            Game = game;
            Solarium = solarium;
            GameTextChannel = gameTextChannel;
            Client = client;
        }

        [NotNull] public virtual Task Start(string address, ChannelCredentials credentials)
        {
            Client.MessageReceived += message => {

                if (message.Author.IsBot)
                    return Task.CompletedTask;

                if (message.Channel.Id != GameTextChannel.Id)
                    return Task.CompletedTask;
                else
                    return HandleUserMessage(message.Author, message.Channel, message.Content);
            };

            return Task.Run(async () => {

                // Open event channel to server
                var channel = new Channel(address, credentials);
                var client = new Solarium.Solarium.SolariumClient(channel);
                var stream = client.GameUpdate(new GameUpdateRequest { GameID = Game.GameId });

                // Block, sending notifications whenever the server says so
                using (var rs = stream.ResponseStream)
                {
                    try
                    {
                        while (await rs.MoveNext())
                        {
                            var n = rs.Current;
                            if (n == null)
                                continue;

                            await HandleEvent(rs.Current);

                            await Task.Delay(150);
                        }
                    }
                    catch (Exception e)
                    {
                        await GameTextChannel.SendMessageAsync($"FATAL EXCEPTION: {e.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Handle a message from someone in the game channel
        /// </summary>
        /// <param name="user"></param>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [NotNull] protected abstract Task HandleUserMessage([NotNull] IUser user, [NotNull] IMessageChannel context, [NotNull] string message);

        /// <summary>
        /// Handle a game event
        /// </summary>
        /// <param name="event"></param>
        /// <returns></returns>
        [NotNull] protected abstract Task HandleEvent([NotNull] GameUpdateResponse @event);

        public void InjectGameEvent([NotNull] IUser user, [NotNull] IMessageChannel context, [NotNull] string message)
        {
            HandleUserMessage(user, context, message);
        }
    }
}
