using System;
using System.Threading.Tasks;
using Mute.Moe.Services.Audio.Clips;

namespace Mute.Moe.Services.Speech.TTS
{
    internal class NullTextToSpeech
        : ITextToSpeech
    {
        public Task<IAudioClip> Synthesize(string text, string? voice = null)
        {
            throw new NotImplementedException();
        }
    }
}
