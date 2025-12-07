using Mute.Moe.Discord.Context.Preprocessing;
using System.Threading.Tasks;
using Mute.Moe.Discord.Context;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Discord.Commands;

/// <summary>
/// Detect "Command words" in Discord audio messages and execute whatever action is associated with them. This is like
/// commands in text messages.
/// </summary>
public class CommandWordService
    : IConversationPreprocessor
{
    private readonly IServiceProvider _provider;
    private readonly Dictionary<StringSpan, Type> _triggerWords = [];

    /// <summary>
    /// Create command words service, automatically finds every <see cref="ICommandWordHandler"/> implementation in this assembly
    /// </summary>
    /// <param name="provider"></param>
    public CommandWordService(IServiceProvider provider)
    {
        _provider = provider;

        var words = from type in Assembly.GetExecutingAssembly().GetTypes()
                    where type.IsAssignableTo(typeof(ICommandWordHandler))
                    select type;

        foreach (var type in words)
        {
            var instance = (ICommandWordHandler?)provider.GetService(type);
            if (instance == null)
                continue;
            
            foreach (var trigger in instance.Triggers)
                _triggerWords.Add(new StringSpan(trigger.AsMemory()), type);
        }
    }

    /// <inheritdoc />
    public async Task Process(MuteCommandContext context)
    {
        // Try to get an audio transcription from this message. Will only exist for audio messages.
        if (!context.TryGet<AudioTranscription>(out var transcription))
            return;

        // Wait for the transcription to finish generating
        var text = await transcription!.GetTranscription();

        // Search for command words and execute them
        var words = text.SplitSpan(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var word in words)
        {
            var trimmed = word
               .TrimStart()
               .TrimEnd(',')
               .TrimEnd('.')
               .TrimEnd('!');

            if (_triggerWords.TryGetValue(new StringSpan(trimmed), out var type))
            {
                var instance = (ICommandWordHandler)_provider.GetRequiredService(type);
                if (await instance.Invoke(context, text))
                {
                    transcription.DisplayTranscription = false;
                    return;
                }
            }
        }
    }

    private readonly struct StringSpan
        : IEquatable<StringSpan>
    {
        private readonly ReadOnlyMemory<char> _memory;
        private readonly int _hashCode;

        public StringSpan(ReadOnlyMemory<char> memory)
        {
            _memory = memory;

            var h = HashCode.Combine(0);
            for (var i = 0; i < memory.Length; i++)
                h = HashCode.Combine(h, memory.Span[i]);
            _hashCode = h;
        }

        public bool Equals(StringSpan other)
        {
            return _memory.Span.SequenceEqual(other._memory.Span);
        }

        public override bool Equals(object? obj)
        {
            return obj is StringSpan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}

/// <summary>
/// Handles "command words" in Discord audio messages (not voice channels, the ones sent inline from the mobile app)
/// </summary>
public interface ICommandWordHandler
{
    /// <summary>
    /// List of all trigger words. if any of these are detected the handler will be run
    /// </summary>
    public IReadOnlyList<string> Triggers { get; }

    /// <summary>
    /// Invoke this command word handler to run for the given context
    /// </summary>
    /// <param name="context"></param>
    /// <param name="transcription"></param>
    /// <returns></returns>
    public Task<bool> Invoke(MuteCommandContext context, string transcription);
}