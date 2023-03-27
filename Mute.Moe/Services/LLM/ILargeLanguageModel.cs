using System.Collections.Generic;

namespace Mute.Moe.Services.LLM
{
    public interface ILargeLanguageModel
    {
        public IReadOnlyList<IGenerationResult> Generate();

        public interface IGenerationResult
        {

        }
    }

    
}
