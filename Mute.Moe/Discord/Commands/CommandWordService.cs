using Mute.Moe.Discord.Context.Preprocessing;
using System.Threading.Tasks;
using Mute.Moe.Discord.Context;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Discord.Commands;

public class CommandWordService
    : IConversationPreprocessor
{
    private readonly IServiceProvider _provider;
    private readonly Dictionary<StringSpan, Type> _triggerWords = [];

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

    public async Task Process(MuteCommandContext context)
    {
        if (!context.TryGet<AudioTranscription>(out var transcription))
            return;

        var text = await transcription!.GetTranscription();

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

public interface ICommandWordHandler
{
    public IReadOnlyList<string> Triggers { get; }

    public Task<bool> Invoke(MuteCommandContext context, string message);
}