namespace Mute.Moe.Services.LLM
{
    internal class NullLLM
        : ILargeLanguageModel
    {
        public override string ToString()
        {
            return "NullLLM";
        }
    }
}
