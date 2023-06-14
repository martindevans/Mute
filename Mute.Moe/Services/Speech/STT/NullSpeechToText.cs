using System;
using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

internal class NullSpeechToText
    : ISpeechToText
{
    public IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio)
    {
        throw new NotImplementedException();
    }
}