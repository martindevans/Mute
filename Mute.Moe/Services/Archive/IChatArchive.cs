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
    /// <param name="context">LLM memory context of this message</param>
    /// <param name="channel">Channel ID this message was sent in</param>
    /// <param name="messageId">Discord ID of this message</param>
    /// <param name="senderId">The Discord ID of the sender</param>
    /// <param name="instant">Instant in time when this message was sent</param>
    /// <param name="content">Text content of the message</param>
    /// <param name="mention">The ID of a message that this message mentions (or null)</param>
    /// <returns>true if record was inserted, false if a record already existed with this message ID</returns>
    Task<bool> Insert(ulong context, ulong channel, ulong messageId, ulong senderId, DateTimeOffset instant, string content, ulong? mention);
}

/// <summary>
/// Extensions to IChatArchive
/// </summary>
public static class IChatArchiveExtensions
{
    /// <summary>
    /// Inserts a message into the chat archive using the provided <see cref="IMessage"/> instance.
    /// </summary>
    /// <param name="archive">The chat archive where the message will be stored.</param>
    /// <param name="socketMessage">The message to be archived, represented as an <see cref="IMessage"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <c>true</c> if the record was inserted, or <c>false</c> if a record with the same message ID already exists.</returns>
    public static Task<bool> Insert(this IChatArchive archive, IMessage socketMessage)
    {
        var reference = default(ulong?);
        if (socketMessage.Reference?.ReferenceType.ToNullable() == MessageReferenceType.Default)
            reference = socketMessage.Reference.MessageId.ToNullable();

        return archive.Insert(
            socketMessage.Channel.GetAgentMemoryContextId(),
            socketMessage.Channel.Id,
            socketMessage.Id,
            socketMessage.Author.Id,
            socketMessage.CreatedAt,
            socketMessage.Content ?? "",
            reference
        );
    }
}