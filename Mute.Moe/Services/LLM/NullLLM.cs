using System;
using Discord;

namespace Mute.Moe.Services.LLM;

internal class NullLLM
    : ILargeLanguageModel
{
    public override string ToString()
    {
        return "NullLLM";
    }

    public async IAsyncEnumerable<string> Generate(string prompt, LargeLanguageModelGenerationOptions? options = null)
    {
        yield break;
    }

    public EmbedBuilder Summary(EmbedBuilder embed)
    {
        return embed.WithTitle("No LLM")
                    .WithTimestamp(DateTimeOffset.UtcNow);
    }
}