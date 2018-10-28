using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Engine
{
    internal class ElizaContext
        : ICommandContext
    {
        public ICommandContext Base { get; }
        public string Input { get; }

        public ElizaContext([NotNull] ICommandContext context, string input)
        {
            Base = context;
            Input = input;
        }

        public IDiscordClient Client => Base.Client;

        public IGuild Guild => Base.Guild;

        public IMessageChannel Channel => Base.Channel;

        public IUser User => Base.User;

        public IUserMessage Message => Base.Message;
    }
}
