using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Mute.Moe.Services.Audio.Mixing.Extensions;
using NAudio.Wave;

namespace Mute.Moe.Services.Speech.STT;

public class MicrosoftCognitiveSpeechToText
    : ISpeechToText
{
    private readonly string _key;
    private readonly string _region;

    public MicrosoftCognitiveSpeechToText(Configuration config)
    {
        _key = config.TTS?.MsCognitive?.Key ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Key));
        _region = config.TTS?.MsCognitive?.Region ?? throw new ArgumentNullException(nameof(config.TTS.MsCognitive.Region));
    }

    public async IAsyncEnumerable<RecognitionWord> ContinuousRecognition(IWaveProvider audioSource, [EnumeratorCancellation] CancellationToken cancellation, IAsyncEnumerable<string>? sourceLangs, IAsyncEnumerable<string>? phrases)
    {
        var config = SpeechConfig.FromSubscription(_key, _region);
        var audioConfig = AudioConfig.FromStreamInput(new PullAdapter(audioSource, 24000), AudioStreamFormat.GetWaveFormatPCM(24000, 16, 1));

        using var recogniser = new SpeechRecognizer(config,
            AutoDetectSourceLanguageConfig.FromLanguages(await (sourceLangs ?? Array.Empty<string>().ToAsyncEnumerable()).Append("en-GB").ToArrayAsync(cancellation)),
            audioConfig
        );

        // Add some likely words to the phrase dictionary
        var phraseList = PhraseListGrammar.FromRecognizer(recogniser);
        phraseList.AddPhrase("mute");
        phraseList.AddPhrase("discord");
        phraseList.AddPhrase("stop");
        if (phrases != null)
            await foreach (var phrase in phrases.WithCancellation(cancellation))
                phraseList.AddPhrase(phrase);

        // Subscribe to recogniser results
        var results = new ConcurrentQueue<RecognitionWord>();
        recogniser.Recognized += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
                results.Enqueue(new RecognitionWord(e.Result.Text));
            else if (e.Result.Reason == ResultReason.NoMatch)
                results.Enqueue(new RecognitionWord(null));
        };

        recogniser.Canceled += (_, e) =>
        {
            Console.WriteLine($"CANCELED: Reason={e.Reason}");

            if (e.Reason == CancellationReason.Error)
            {
                results.Enqueue(new RecognitionWord($"CANCELED: ErrorCode={e.ErrorCode}"));
                results.Enqueue(new RecognitionWord($"CANCELED: ErrorDetails={e.ErrorDetails}"));
                //results.Enqueue(new RecognitionWord($"CANCELED: Did you update the subscription info?"));
            }
        };

        recogniser.SessionStarted += (_, _) =>
        {
            results.Enqueue(new RecognitionWord("Session_started_event."));
        };

        var stopped = false;
        recogniser.SessionStopped += (_, _) =>
        {
            results.Enqueue(new RecognitionWord("Session_stopped_event."));
            stopped = true;
        };

        // Return recognised results until cancelled
        await recogniser.StartContinuousRecognitionAsync();
        while (!cancellation.IsCancellationRequested && !stopped)
            if (results.TryDequeue(out var r))
                yield return r;

        // Stop receiving further results
        await recogniser.StopContinuousRecognitionAsync();

        // Finish sending remaining results
        foreach (var result in results)
            yield return result;
    }

    private class PullAdapter
        : PullAudioInputStreamCallback
    {
        private readonly IWaveProvider _provider;

        public PullAdapter(IWaveProvider provider, int sampleRate)
        {
            _provider = provider.ToSampleProvider().ToMono().Resample(sampleRate).ToWaveProvider16();
        }

        public override int Read(byte[] dataBuffer, uint size)
        {
            while (true)
            {
                var r = _provider.Read(dataBuffer, 0, (int)size);
                if (r > 0)
                    return r;

                Thread.Sleep(50);
            }
        }
    }
}