using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

/// <summary>
/// Convert an audio clip to text
/// </summary>
public interface ISpeechToText
{
    /// <summary>
    /// Consume the entire sample provider until it finishes, and return string
    /// </summary>
    /// <param name="audio"></param>
    /// <returns></returns>
    IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio);
}

/// <summary>
/// Speech-To-Text recognition part. May be a single word, if recognition is streaming
/// </summary>
/// <param name="Text"></param>
public readonly record struct RecognitionWord(string? Text);