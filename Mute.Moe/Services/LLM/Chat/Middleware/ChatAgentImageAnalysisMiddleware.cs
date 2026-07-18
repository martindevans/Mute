using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Services.ImageGen;

namespace Mute.Moe.Services.LLM.Chat.Middleware;

/// <summary>
/// Middleware for processing chat messages containing images by replacing them with text descriptions.
/// </summary>
/// <remarks>
/// This middleware utilizes an <see cref="IImageAnalyser"/> to analyze images and generate descriptive text.
/// It processes chat messages, identifies image content, and replaces it with a description while maintaining
/// the conversational flow.
/// </remarks>
public class ChatAgentImageAnalysisMiddleware
{
    private readonly IImageAnalyser _analyser;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAgentImageAnalysisMiddleware"/> class.
    /// </summary>
    /// <param name="analyser">
    /// The image analyser used to process and generate descriptions for images.
    /// </param>
    public ChatAgentImageAnalysisMiddleware(IImageAnalyser analyser)
    {
        _analyser = analyser;
    }
    
    /// <summary>
    /// Replaces message with images with a message with a text description
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="session"></param>
    /// <param name="options"></param>
    /// <param name="innerAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AgentResponse> Middleware(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        CancellationToken cancellationToken)
    {
        return await innerAgent.RunAsync(
            await ModifyMessages(messages, cancellationToken).ToListAsync(cancellationToken),
            session,
            options,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces message with images with a message with a text description
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="session"></param>
    /// <param name="options"></param>
    /// <param name="innerAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<AgentResponseUpdate> MiddlewareStreaming(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent innerAgent,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var results = innerAgent.RunStreamingAsync(
            await ModifyMessages(messages, cancellationToken).ToListAsync(cancellationToken),
            session,
            options,
            cancellationToken
        );

        await foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return result;
        }
    }

    private async IAsyncEnumerable<ChatMessage> ModifyMessages(IEnumerable<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellation)
    {
        foreach (var message in messages)
            yield return await ReplaceMessage(message, cancellation);
    }
    
    private async Task<ChatMessage> ReplaceMessage(ChatMessage message, CancellationToken cancellation)
    {
        for (var i = 0; i < message.Contents.Count; i++)
        {
            // Find data
            var content = message.Contents[i];
            if (content is not DataContent dc)
                continue;

            // That represents an image
            if (!dc.HasTopLevelMediaType("image"))
                continue;

            // Get a description
            var bytes = Convert.FromBase64String(new string(dc.Base64Data.Span));
            var stream = new MemoryStream(bytes);
            var desc = await _analyser.GetImageDescription(stream, cancellation);

            // Replace contents
            message.Contents[i] = new TextContent($"An image was provided by the user, it has been converted into an automatic description of " +
                                                  $"the image. Do not mention this conversion unless directly asked about it by the user, instead " +
                                                  $"act as if you can see the image directly\n\n**{desc.Title}**\n{desc.Description}");
        }

        return message;
    }
}