using Mute.Moe.Services.Audio.Mixing.Extensions;
using Mute.Moe.Services.Notifications.Cron;
using NAudio.Wave;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Whisper.Runtime;
using static Whisper.Runtime.WhisperRuntime;

namespace Mute.Moe.Services.Speech.STT;

/// <inheritdoc />
public class WhisperSpeechToText
    : ISpeechToText
{
    private readonly uint _threads;
    private readonly string _model;
    private readonly TimeSpan _modelUnloadTimeout;

    private DateTime _lastUse;
    private whisper_context? _context;
    private readonly object _contextLock = new();
    
    /// <summary>
    /// Create a new whisper STT processor
    /// </summary>
    /// <param name="config"></param>
    /// <param name="cron"></param>
    /// <exception cref="FileNotFoundException"></exception>
    public WhisperSpeechToText(Configuration config, ICron cron)
    {
        ArgumentNullException.ThrowIfNull(config.STT?.Whisper, nameof(config.STT.Whisper));

        var model = config.STT.Whisper.ModelPath;
        ArgumentNullException.ThrowIfNull(model, nameof(config.STT.Whisper.ModelPath));
        if (!File.Exists(model))
            throw new FileNotFoundException("Whisper model not found", model);

        _threads = config.STT?.Whisper?.Threads ?? (uint)(Environment.ProcessorCount / 2);
        _threads = Math.Clamp(_threads, 1, (uint)Environment.ProcessorCount);

        _modelUnloadTimeout = TimeSpan.FromMinutes(2);
        cron.Interval(_modelUnloadTimeout / 2, AutoCleanupContext, int.MaxValue);

        _model = model;
    }

    /// <inheritdoc />
    public IEnumerable<RecognitionWord> OneShotRecognition(ISampleProvider audio)
    {
        lock (_contextLock)
        {
            var context = GetContext();

            using var state = whisper_init_state(context);
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
                var ret = whisper_full_with_state(context, state, parameters, chunk.Array, chunk.Count);
                if (ret != 0)
                    return [ ];
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

    private async Task AutoCleanupContext()
    {
        if (_context == null)
            return;

        // If we can't take the lock just early exit, we'll try again later
        if (!Monitor.TryEnter(_contextLock))
            return;

        try
        {
            // Check elapsed time since the resource was last used
            if (DateTime.UtcNow - _lastUse < _modelUnloadTimeout)
                return;

            // Free the model
            Log.Information("Auto cleanup of whisper_context");
            whisper_free(_context);
            _context = null;
        }
        finally
        {
            Monitor.Exit(_contextLock);
        }
    }

    private whisper_context GetContext()
    {
        _context ??= whisper_init_from_file(_model);
        _lastUse = DateTime.UtcNow;
        return _context;
    }
}