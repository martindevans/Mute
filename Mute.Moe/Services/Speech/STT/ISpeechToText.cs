using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

public interface ISpeechToText
{
    //IAsyncEnumerable<RecognitionWord> ContinuousRecognition(ISampleProvider audio, CancellationToken cancellation, IAsyncEnumerable<string>? sourceLangs, IAsyncEnumerable<string>? phrases);

    IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio);
}

public readonly struct RecognitionWord
{
    public string? Text { get; }

    public RecognitionWord(string? text)
    {
        Text = text;
    }
}