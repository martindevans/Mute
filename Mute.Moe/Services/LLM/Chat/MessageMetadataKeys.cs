namespace Mute.Moe.Services.LLM.Chat;

/// <summary>
/// Keys for metadata to attach to messages
/// </summary>
public static class MessageMetadataKeys
{
    /// <summary>
    /// The numeric ID (ulong) of the author of this message
    /// </summary>
    public const string U64_DiscordAuthorId = nameof(MessageMetadataKeys) + "." + nameof(U64_DiscordAuthorId);
    
    /// <summary>
    /// The time (ulong) this message was sent (unix timestamp)
    /// </summary>
    public const string U64_Timestamp = nameof(MessageMetadataKeys) + "." + nameof(U64_Timestamp);

    /// <summary>
    /// The numeric ID (ulong) of the discord channel ID this message was sent in
    /// </summary>
    public const string U64_DiscordChannelId = nameof(MessageMetadataKeys) + "." + nameof(U64_DiscordChannelId);
}