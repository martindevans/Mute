using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using JetBrains.Annotations;
using Mute.Moe.Services.Audio.Mixing;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Discord.Services.Audio.Playback
{
    internal class MultichannelAudioPlayer
    {
        public static readonly WaveFormat MixingFormat = WaveFormat.CreateIeeeFloatWaveFormat(24000, 1);
        public static readonly WaveFormat OutputFormat = new WaveFormat(48000, 16, 2);

        private readonly IVoiceChannel _voiceChannel;
        private readonly AutoResetEvent _threadEvent;
        private readonly Task _thread;
        private readonly byte[] _buffer = new byte[MixingFormat.AverageBytesPerSecond / 25];

        private readonly Dictionary<IChannel, ChannelConverter> _channels = new Dictionary<IChannel, ChannelConverter>();

        private readonly MixingSampleProvider _mixerInput;
        private readonly IWaveProvider _mixerOutput;

        private volatile bool _stopped;

        public MultichannelAudioPlayer([NotNull] IVoiceChannel voiceChannel, [NotNull] IEnumerable<IChannel> sources)
        {
            _voiceChannel = voiceChannel;
            _threadEvent = new AutoResetEvent(true);
            _thread = Task.Run(ThreadEntry);

            //Sample provider which mixes together several sample providers
            _mixerInput = new MixingSampleProvider(MixingFormat) { ReadFully = true };

            //Soft clip using opus
            var clipper = new SoftClipSampleProvider(_mixerInput.ToMono());

            //resample mix format to output format
            _mixerOutput = new WdlResamplingSampleProvider(clipper, OutputFormat.SampleRate).ToStereo().ToWaveProvider16();

            //Add all initial channels to the mixer
            foreach (var source in sources)
                Add(source);
        }

        public void Add([NotNull] IChannel source)
        {
            var converter = new ChannelConverter(source);

            lock (_channels)
            lock (_mixerInput)
            {
                _channels.Add(source, converter);
                _mixerInput.AddMixerInput(converter);
            }
        }

        public void Remove(IChannel source)
        {
            lock (_channels)
            lock (_mixerInput)
            {
                if (_channels.TryGetValue(source, out var converter))
                    _mixerInput.RemoveMixerInput(converter);
            }
        }

        public async Task Stop()
        {
            _stopped = true;
            _threadEvent.Set();

            await Task.Run(() => {
                while (!_thread.IsCompleted)
                    Thread.Sleep(1);
            });
        }

        private async Task ThreadEntry()
        {
            try
            {
                using (var c = await _voiceChannel.ConnectAsync())
                using (var s = c.CreatePCMStream(AudioApplication.Mixed, _voiceChannel.Bitrate))
                {
                    var speakingState = false;
                    while (!_stopped)
                    {
                        //Wait for an event to happen to wake up the thread
                        if (!speakingState)
                            _threadEvent.WaitOne(250);

                        //Break out if stop flag has been set
                        if (_stopped)
                            return;

                        //Count up how many channels are playing.
                        bool playing;
                        lock (_channels)
                            playing = _channels.Select(a => a.Value.IsPlaying ? 1 : 0).Sum() > 0;

                        //Set playback state if it has changed
                        if (playing != speakingState)
                        {
                            await c.SetSpeakingAsync(speakingState);
                            speakingState = playing;
                        }

                        //Early exit if nothing is playing
                        if (!speakingState)
                            continue;

                        //Copy mixed audio to the output
                        await WriteOutput(_mixerOutput, s, _buffer.Length, _buffer);
                    }

                    await c.SetSpeakingAsync(false);
                    await c.StopAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static async Task WriteOutput(IWaveProvider waveSource, AudioOutStream waveSink, int sampleCount, byte[] buffer)
        {
            while (sampleCount > 0)
            {
                //Read output from mixer
                var mixed = waveSource.Read(buffer, 0, buffer.Length);
                sampleCount -= mixed;

                //Send the mixed audio buffer to discord
                await waveSink.WriteAsync(buffer, 0, mixed);
                
                //If no audio was mixed early exit, this probably indicates the end of the stream
                if (mixed == 0)
                    return;
            }
        }

        /// <summary>
        /// Resamples a source channels to the mixing rate (and mono)
        /// </summary>
        private class ChannelConverter
            : ISampleProvider
        {
            [NotNull] private readonly IChannel _channel;

            [NotNull] private readonly ISampleProvider _resampled;

            public bool IsPlaying => _channel.IsPlaying;

            public ChannelConverter([NotNull] IChannel channel)
            {
                _channel = channel;

                var resampler = new WdlResamplingSampleProvider(channel.ToMono(), MixingFormat.SampleRate);
                _resampled = resampler;
            }

            public int Read(float[] buffer, int offset, int count)
            {
                if (!IsPlaying)
                {
                    Array.Clear(buffer, offset, count);
                    return count;
                }
                else
                {
                    var c = _resampled.Read(buffer, offset, count);
                    return c;
                }
            }

            public WaveFormat WaveFormat => _resampled.WaveFormat;
        }
    }
}
