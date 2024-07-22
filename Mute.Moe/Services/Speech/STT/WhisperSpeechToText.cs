using System.IO;
using System.Text;
using Mute.Moe.Services.Audio.Mixing.Extensions;
using NAudio.Wave;
using Whisper.Runtime;
using static Whisper.Runtime.WhisperRuntime;

namespace Mute.Moe.Services.Speech.STT;

public class WhisperSpeechToText
    : ISpeechToText
{
    private readonly whisper_context _context;
    private readonly uint _threads;

    public WhisperSpeechToText(Configuration config)
    {
        ArgumentNullException.ThrowIfNull(config.STT?.Whisper, nameof(config.STT.Whisper));

        var model = config.STT.Whisper.ModelPath;
        ArgumentNullException.ThrowIfNull(model, nameof(config.STT.Whisper.ModelPath));
        if (!File.Exists(model))
            throw new FileNotFoundException("Whisper model not found", model);

        _threads = config.STT?.Whisper?.Threads ?? Math.Max(1, (uint)(Environment.ProcessorCount * 0.5));

        _context = whisper_init_from_file(model);
    }

    public IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio)
    {
        using var state = whisper_init_state(_context);
        using var parameters = whisper_full_default_params(whisper_sampling_strategy.WHISPER_SAMPLING_BEAM_SEARCH);
        parameters.print_realtime = false;
        parameters.print_progress = false;
        parameters.print_timestamps = false;
        parameters.translate = false;
        parameters.n_threads = (int)_threads;
        parameters.offset_ms = 0;
        parameters.strategy = whisper_sampling_strategy.WHISPER_SAMPLING_GREEDY;
        parameters.no_context = true;
        parameters.suppress_non_speech_tokens = false;

        var output = new StringBuilder();

        foreach (var chunk in ReadChunks(audio))
        {
            var ret = whisper_full_with_state(_context, state, parameters, chunk.Array, chunk.Count);
            if (ret != 0)
                return Array.Empty<RecognitionWord>();
            parameters.no_context = false;

            var segmentsCount = whisper_full_n_segments_from_state(state);
            for (var i = 0; i < segmentsCount; i++)
            {
                var text = whisper_full_get_segment_text_from_state(state, i);
                output.Append(text);
            }
        }

        return output.ToString().Split(" ").Select(a => new RecognitionWord(a));
    }

    private static IEnumerable<ArraySegment<float>> ReadChunks(ISampleProvider audio)
    {
        var samples = audio.ToMono().Resample(16000);

        // 30 second frame
        var frame = new float[480000];

        while (true)
        {
            var read = samples.Read(frame, 0, frame.Length);
            yield return new ArraySegment<float>(frame, 0, read);

            if (read != frame.Length)
                break;
        }
    }
}