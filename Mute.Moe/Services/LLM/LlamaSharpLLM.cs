using System.IO;
using Discord;
using LLama;
using LLama.Common;

namespace Mute.Moe.Services.LLM;

public class LlamaSharpLLM
    : ILargeLanguageModel
{
    private readonly ModelParams _modelParams;
    private readonly InferenceParams _inferParams;

    public LlamaSharpLLM(Configuration config)
    {
        _modelParams = new ModelParams(modelPath: config.LLM?.ModelPath ?? throw new InvalidOperationException("No LLM model supplied"));

        _inferParams = new InferenceParams
        {
            AntiPrompts = new[] { "\\end", "User:" },
            MaxTokens = 512,
            Mirostat = MirostatType.Mirostat2
        };
    }

    public async IAsyncEnumerable<string> Generate(string prompt)
    {
        using var model = LLamaWeights.LoadFromFile(_modelParams);
        using var ctx = model.CreateContext(_modelParams);

        var executor = new InteractiveExecutor(ctx);

        await foreach (var item in executor.InferAsync(prompt, _inferParams))
            yield return item;
    }

    public EmbedBuilder Summary(EmbedBuilder embed)
    {
        return embed.WithTitle("llama.cpp")
                    .AddField("Model", Path.GetFileNameWithoutExtension(_modelParams.ModelPath))
                    .AddField("Context Size", _modelParams.ContextSize);
    }
}