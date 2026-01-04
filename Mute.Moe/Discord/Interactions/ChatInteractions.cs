using Discord.Interactions;
using Mute.Moe.Discord.Services.Responses;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Interactions;

/// <summary>
/// Interactions to do with LLM chat
/// </summary>
/// <param name="_conversations"></param>
public partial class ChatInteractions(ConversationalResponseService _conversations)
    : MuteInteractionModuleBase
{
    /// <summary>
    /// ID for clearing the conversation state in the current channel
    /// </summary>
    public const string InteractionIdClearConversationState = nameof(ChatInteractions) + nameof(InteractionIdClearConversationState);

    /// <summary>
    /// ID for forcing summarisation of the conversation state in the current channel
    /// </summary>
    public const string InteractionIdSummariseConversationState = nameof(ChatInteractions) + nameof(InteractionIdSummariseConversationState);

    /// <summary>
    /// Clear the conversation state in this channel
    /// </summary>
    /// <returns></returns>
    [ComponentInteraction(InteractionIdClearConversationState, ignoreGroupNames: true)]
    [UsedImplicitly]
    public async Task ClearConversationState()
    {
        await RespondAsync("Clearing...");

        var conv = await _conversations.GetConversation(Context.Channel);
        await conv.Clear();

        await FollowupAsync("Cleared conversation state. Wait... what were we talking about again?");
        await DeleteOriginalResponseAsync();
    }

    /// <summary>
    /// Force summarisation of the conversation state in this channel
    /// </summary>
    /// <returns></returns>
    [ComponentInteraction(InteractionIdSummariseConversationState, ignoreGroupNames: true)]
    [UsedImplicitly]
    public async Task SummariseConversationState()
    {
        await RespondAsync("Summarising...");

        var conv = await _conversations.GetConversation(Context.Channel);
        await conv.ForceSummary();

        await FollowupAsync($"Summarisation completed:\n{conv.Summary}");
        await DeleteOriginalResponseAsync();
    }
}