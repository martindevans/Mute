﻿using System.IO;
using Discord;
using LLama;
using LLama.Common;

namespace Mute.Moe.Services.LLM
{
    public class LlamaSharpLLM
        : ILargeLanguageModel
    {
        private readonly ModelParams _modelParams;
        private readonly InferenceParams _inferParams;

        public LlamaSharpLLM(Configuration config)
        {
            _modelParams = new ModelParams(
                modelPath: config.LLM?.ModelPath ?? throw new InvalidOperationException("No LLM model supplied"),
                contextSize: config.LLM?.ModelContextSize ?? 2048
            );

            _inferParams = new InferenceParams
            {
                AntiPrompts = new[] { "\\end", "User:" },
                MaxTokens = 512,
                Mirostat = MiroStateType.MiroState2
            };
        }

        public IAsyncEnumerable<string> Generate(string prompt)
        {
            var model = new LLamaModel(_modelParams);
            {
                var executor = new InteractiveExecutor(model);
                return executor.InferAsync(prompt, _inferParams);
            }
        }

        public EmbedBuilder Summary(EmbedBuilder embed)
        {
            return embed.WithTitle("llama.cpp")
                        .AddField("Model", Path.GetFileNameWithoutExtension(_modelParams.ModelPath))
                        .AddField("Context Size", _modelParams.ContextSize);
        }
    }
}