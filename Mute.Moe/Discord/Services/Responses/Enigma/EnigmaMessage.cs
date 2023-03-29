using Discord;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Services.Responses.Enigma;

public class EnigmaMessage
{
    public ulong Speaker { get; }
    public string Message { get; }

    public EnigmaMessage(ulong speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }

    public static EnigmaMessage From(MuteCommandContext context)
    {
        var content = context.Message.Resolve(TagHandling.NameNoPrefix, TagHandling.FullName, TagHandling.NameNoPrefix, TagHandling.NameNoPrefix, TagHandling.FullName);
        return new EnigmaMessage(context.User.Id, content);
    }

    public static EnigmaMessage From(ulong userId, string content)
    {
        return new EnigmaMessage(userId, content);
    }
}