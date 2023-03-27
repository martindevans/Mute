using System;
using System.Collections.Generic;

namespace Mute.Moe.Services.LLM
{
    internal class NullLLM
        : ILargeLanguageModel
    {
        public override string ToString()
        {
            return "NullLLM";
        }

        public IReadOnlyList<ILargeLanguageModel.IGenerationResult> Generate()
        {
            return Array.Empty<ILargeLanguageModel.IGenerationResult>();
        }
    }
}
