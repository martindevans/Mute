using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

public interface ISpeechToText
{
    //IAsyncEnumerable<RecognitionWord> ContinuousRecognition(ISampleProvider audio, CancellationToken cancellation, IAsyncEnumerable<string>? sourceLangs, IAsyncEnumerable<string>? phrases);

    IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio);
}

public readonly record struct RecognitionWord(string? Text);