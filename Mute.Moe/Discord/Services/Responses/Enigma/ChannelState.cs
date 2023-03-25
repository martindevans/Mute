using System;
using Mute.Moe.Services.LLM;

namespace Mute.Moe.Discord.Services.Responses.Enigma
{
    public class ChannelState
    {
        private readonly ILargeLanguageModel _llm;
        private readonly int _seed;

        public DateTime LastUpdate { get; private set; }

        public ChannelState(ILargeLanguageModel llm, int seed)
        {
            _llm = llm;
            _seed = seed;

            LastUpdate = DateTime.UtcNow;
        }

        public override string ToString()
        {
            var data = new
            {
                llm = _llm.ToString(),
                LastUpdate,
                Seed = _seed
            };

            return data.ToString() ?? string.Empty;
        }

        public string? TryReply(string message)
        {
            LastUpdate = DateTime.UtcNow;

            return null;
        }
    }
}
