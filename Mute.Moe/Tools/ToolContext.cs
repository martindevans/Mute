using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Microsoft.Agents.AI;

namespace Mute.Moe.Tools;

/// <summary>
/// Context which can be retrieved from AgentRunOptions calls
/// </summary>
public class MuteAgentContext
{
    /// <summary>
    /// The channel that this agent is responding in
    /// </summary>
    public IChannel Channel { get; }

    /// <summary>
    /// Check if the current context allows NSFW content
    /// </summary>
    public bool IsNsfw => Channel is IDMChannel or ITextChannel { IsNsfw: true };

    /// <summary>
    /// Get the memory context ID
    /// </summary>
    public ulong MemoryContext => Channel.GetAgentMemoryContextId();

    /// <summary>
    /// Construct a new instance
    /// </summary>
    /// <param name="channel"></param>
    public MuteAgentContext(IChannel channel)
    {
        Channel = channel;
    }

    /// <summary>
    /// Get the MuteAgentContext from the amibent <see cref="AIAgent.CurrentRunContext"/>
    /// </summary>
    /// <returns></returns>
    public static Task<MuteAgentContext?> GetContext(IDiscordClient client)
    {
        return AIAgent.CurrentRunContext?.RunOptions?.GetMuteContext(client)
            ?? Task.FromResult<MuteAgentContext?>(null);
    }
}

/// <summary>
/// Json serialisable data for the agent context
/// </summary>
/// <param name="ChannelId"></param>
[JsonSerializable(typeof(MuteAgentContextModel))]
public record MuteAgentContextModel(
    ulong ChannelId
);

/// <summary>
/// Extensions for <see cref="AgentRunOptions"/>
/// </summary>
public static class AgentRunOptionsExtensions
{
    #region ambient context storage
    private const string Key = "37177121-CF9D-444E-939A-A886FA9E7D9A";

    /// <summary>
    /// Get the current tool context
    /// </summary>
    /// <returns></returns>
    public static async Task<MuteAgentContext?> GetMuteContext(this AgentRunOptions? options, IDiscordClient client)
    {
        if (options?.AdditionalProperties?.GetValueOrDefault(Key, null) is not MuteAgentContextModel model)
            return null;

        var channel = await client.GetChannelAsync(model.ChannelId);
        if (channel == null)
            return null;

        return new MuteAgentContext(
            channel
        );
    }

    /// <summary>
    /// Attach the tool context to the AI agent run options
    /// </summary>
    public static void AttachMuteContext(this AgentRunOptions options, MuteAgentContext context)
    {
        options.AdditionalProperties ??= new();
        options.AdditionalProperties[Key] = new MuteAgentContextModel(context.Channel.Id);
    }
    #endregion
}