using System;
using System.Collections.Generic;
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