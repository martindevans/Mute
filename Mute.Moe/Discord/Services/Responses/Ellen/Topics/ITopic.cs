using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Services.Responses.Ellen.Knowledge;

namespace Mute.Moe.Discord.Services.Responses.Ellen.Topics;

/// <summary>
/// A provider of new topics for the chat script
/// </summary>
public interface ITopicProvider
{
    /// <summary>
    /// A set of topic keys provided by this
    /// </summary>
    IEnumerable<ITopic> Keys { get; }
}

/// <summary>
/// A potential topic the bot can talk about
/// </summary>
public interface ITopic
{
    /// <summary>
    /// Get the keywords which will trigger an attempt to use this key
    /// </summary>
    IReadOnlyList<string> Keywords { get; }

    /// <summary>
    /// Get the rank of this topic, higher ranks will be matched first
    /// </summary>
    int Rank { get; }

    /// <summary>
    /// Try to start a conversation on this topic in response to the given message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="knowledge"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<ITopicDiscussion?> TryBegin(MuteCommandContext message, IKnowledge knowledge, CancellationToken ct);
}

/// <summary>
/// A discussion regarding a specific topic
/// </summary>
public interface ITopicDiscussion
{
    bool IsComplete { get; }

    Task<(string?, IKnowledge)> Reply(IKnowledge knowledge, MuteCommandContext message);
}