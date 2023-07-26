using Discord;

namespace Mute.Moe.Services.LLM;

public interface ILargeLanguageModel
{
    public IAsyncEnumerable<string> Generate(string prompt);

    EmbedBuilder Summary(EmbedBuilder embed);
}