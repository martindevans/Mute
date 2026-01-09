using Mute.Moe.Services.Database;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Stores per-channel conversation state
/// </summary>
public interface IConversationStateStorage
    : IKeyValueStorage<ConversationStateData>;

/// <summary>
/// Wrapper for conversation state data
/// </summary>
/// <param name="Json"></param>
public record ConversationStateData(string Json);

/// <summary>
/// Stores per-channel conversation state
/// </summary>
public class ConversationStateStorage(IDatabaseService database)
    : SimpleJsonBlobTable<ConversationStateData>("ConversationStateStorage", database), IConversationStateStorage;