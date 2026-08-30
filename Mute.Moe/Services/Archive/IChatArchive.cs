using Discord;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Archive;

/// <summary>
/// Archive of all past chat messages
/// </summary>
public interface IChatArchive
{
    /// <summary>
    /// Add a new message to the archive
    /// </summary>
    /// <param name="context">LLM memroy context of this message</param>
    /// <param name="channel">Channel ID this message was sent in</param>
    /// <param name="messageId">Discord ID of this message</param>
    /// <param name="instant">Instant in time when this message was sent</param>
    /// <param name="content">Text content of the message</param>
    /// <param name="mention">The ID of a message that this message mentions (or null)</param>
    /// <returns>true if record was inserted, false if a record already existed with this message ID</returns>
    Task<bool> Insert(ulong context, ulong channel, ulong messageId, DateTimeOffset instant, string content, ulong? mention);
}

/// <summary>
/// Extensions to IChatArchive
/// </summary>
public static class IChatArchiveExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="archive"></param>
    /// <param name="socketMessage"></param>
    /// <returns></returns>
    public static Task<bool> Insert(this IChatArchive archive, IMessage socketMessage)
    {
        var reference = default(ulong?);
        if (socketMessage.Reference?.ReferenceType.ToNullable() == MessageReferenceType.Default)
            reference = socketMessage.Reference.MessageId.ToNullable();

        return archive.Insert(
            socketMessage.Channel.GetAgentMemoryContextId(),
            socketMessage.Channel.Id,
            socketMessage.Id,
            socketMessage.CreatedAt,
            socketMessage.Content ?? "",
            reference
        );
    }
}