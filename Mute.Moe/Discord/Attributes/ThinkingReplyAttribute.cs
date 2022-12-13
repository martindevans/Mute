﻿using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// An emoji will be attached to the message which triggered this context for the duration of the response handler
/// </summary>
public class ThinkingReplyAttribute
    : BaseExecuteContextAttribute
{
    private readonly IEmote _emote;

    public ThinkingReplyAttribute(string emote = EmojiLookup.Thinking)
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