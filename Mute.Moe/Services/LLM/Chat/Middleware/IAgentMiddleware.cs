using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Chat.Middleware;

/// <summary>
/// Interface for chat middleware to add to an agent
/// </summary>
public interface IAgentMiddleware
{
    /// <summary>
    /// Non-streaming middleware
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="session"></param>
    /// <param name="options"></param>
    /// <param name="innerAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<AgentResponse> Middleware(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Streaming middleware
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="session"></param>
    /// <param name="options"></param>
    /// <param name="innerAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<AgentResponseUpdate> MiddlewareStreaming(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken
    );
}