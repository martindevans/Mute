using System.Threading;
using System.Threading.Tasks;
using HandyAgentFramework.Compaction;
using Humanizer;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mute.Moe.Services.LLM.Chat.Context;

/// <summary>
/// Adds a message to chat indicating the amount of elapsed time since the last message
/// </summary>
/// <param name="Threshold">The message is only inserted if the elapsed time is greater than this</param>
public class TimeSinceLastMessageContextProvider(TimeSpan Threshold)
    : AIContextProvider
{
    /// <inheritdoc />
    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        // Get the history
        if (context.Session == null || !context.Session.TryGetInMemoryChatHistory(out var history) || history.Count == 0)
            return new AIContext();

        // Get the most recent message
        var last = history[^1];

        // Exit if we don't know when it was created
        var time = last.CreatedAt;
        if (time == null)
            return new AIContext();

        // Exit if it was created recently
        var elapsed = DateTimeOffset.UtcNow - time.Value;
        if (elapsed < Threshold)
            return new AIContext();

        // Inject a fake tool call, informing the AI that some time has passed
        var guid = Guid.NewGuid().ToString();
        return new AIContext
        {
            Messages =
            [
                new ChatMessage(ChatRole.Tool, [
                    new FunctionCallContent(guid, "get_time_since_last_message"),
                    new FunctionResultContent(guid, new { Elapsed = elapsed.Humanize() })
                ]) {
                    AdditionalProperties = new()
                    {
                        { EphemeralMessageCompaction.IsEphemeralMarker, true },
                        { EphemeralMessageCompaction.EphemeralGroupId, nameof(TimeSinceLastMessageContextProvider) }
                    }
                }
            ]
        };
    }
}