using System.Collections.Generic;
using Discord;

namespace Mute.Moe.Services.LLM
{
    public interface ILargeLanguageModel
    {
        public IAsyncEnumerable<string> Generate(string prompt, LargeLanguageModelGenerationOptions? options = null);

        EmbedBuilder Summary(EmbedBuilder embed);
    }

    public record LargeLanguageModelGenerationOptions
    {
        public int MaxTokens { get; set; } = 128;
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public float? TopK { get; set; }

        /// <summary>
        /// Override values of this with values of the given values, where they are not null
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual LargeLanguageModelGenerationOptions Merge(LargeLanguageModelGenerationOptions? options)
        {
            return new()
            {
                MaxTokens = options?.MaxTokens ?? MaxTokens,
                Temperature = options?.Temperature ?? Temperature,
                TopP = options?.TopP ?? TopP,
                TopK = options?.TopK ?? TopK,
            };
        }
    }
}
