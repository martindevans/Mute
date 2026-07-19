using System.ClientModel;
using Microsoft.Extensions.AI;
using MultiBackendServiceProvider;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;

namespace Mute.Moe.Services.LLM.Client;

/// <summary>
/// Chat client with routes requests to upstream clients
/// </summary>
public sealed class MultiBackendChatClient
    : IChatClient
{
    private readonly MultiBackendServiceProvider<LLamaServerEndpoint> _llamaProvider;

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiBackendChatClient"/> class.
    /// </summary>
    /// <param name="llamaProvider">Provides a llama-server instance</param>
    public MultiBackendChatClient(MultiBackendServiceProvider<LLamaServerEndpoint> llamaProvider)
    {
        _llamaProvider = llamaProvider;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(MultiBackendChatClient));

        using var scope = await AcquireScope(options, cancellationToken);
        return await scope.Client.GetResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(MultiBackendChatClient));

        using var scope = await AcquireScope(options, cancellationToken);
        await foreach (var item in scope.Client.GetStreamingResponseAsync(messages, options, cancellationToken))
            yield return item;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    private async Task<ClientScope> AcquireScope(ChatOptions? options, CancellationToken cancellation)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, typeof(MultiBackendChatClient));
        ArgumentNullException.ThrowIfNull(options?.ModelId, nameof(options));
        ArgumentNullException.ThrowIfNull(options);

        var modelId = options.ModelId;
        if (modelId == null)
            throw new ArgumentException("Must specify a model name", nameof(options));

        var tags = new[] { options.ModelId };
        var server = await _llamaProvider.Acquire(tags, cancellation);
        if (server == null)
            throw new FailedToAcquireScope();

        var client = new OpenAIClient(
                         new ApiKeyCredential(server.Backend.Value.Key),
                         new OpenAIClientOptions { Endpoint = new Uri(server.Backend.Value.Url) }
                     )
                    .GetChatClient(modelId)
                    .AsIChatClient();


        return new ClientScope(client, server);
    }

    private class ClientScope
        : IDisposable
    {
        private readonly Backend<LLamaServerEndpoint>.IScope _scope;
        
        public IChatClient Client { get; }

        public ClientScope(IChatClient client, Backend<LLamaServerEndpoint>.IScope scope)
        {
            _scope = scope;
            Client = client;
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
    
    /// <summary>
    /// Failed to acquire a scope from any backend LLM provider
    /// </summary>
    public class FailedToAcquireScope : Exception;
}