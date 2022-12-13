using System.Collections.Generic;
using System.Threading;
using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

public interface ISpeechToText
{
    IAsyncEnumerable<RecognitionWord> ContinuousRecognition(IWaveProvider audio, CancellationToken cancellation, IAsyncEnumerable<string>? sourceLangs, IAsyncEnumerable<string>? phrases);
}

public readonly struct RecognitionWord
{
    public string? Text { get; }

    public RecognitionWord(string? text)
    {
        Text = text;
    }
}