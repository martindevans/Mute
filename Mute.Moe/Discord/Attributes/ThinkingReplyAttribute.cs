using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes
{
    public class ThinkingReplyAttribute
        : BaseExecuteContextAttribute
    {
        private readonly IEmote _emote;

        public ThinkingReplyAttribute(string emote = "🤔")
        {
            _emote = new Emoji(emote);
        }

        protected internal override IEndExecute StartExecute(MuteCommandContext context)
        {
            context.Message.AddReactionAsync(_emote);

            return new EndExecute(context.Message, _emote, context.Client.CurrentUser);
        }

        private class EndExecute
            : IEndExecute
        {
            private readonly IUserMessage _message;
            private readonly IEmote _emote;
            private readonly IUser _self;

            public EndExecute(IUserMessage message, IEmote emote, IUser self)
            {
                _message = message;
                _emote = emote;
                _self = self;
            }

            Task IEndExecute.EndExecute()
            {
                return _message.RemoveReactionAsync(_emote, _self);
            }
        }
    }
}
