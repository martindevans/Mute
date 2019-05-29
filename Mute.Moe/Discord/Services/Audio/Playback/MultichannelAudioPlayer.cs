using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using JetBrains.Annotations;
using Mute.Moe.Services.Audio.Mixing;
using NAudio.Wave;

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

        private volatile bool _stopped;

        private readonly MultiChannelMixer _mixer;

        public MultichannelAudioPlayer([NotNull] IVoiceChannel voiceChannel)
        {
            _voiceChannel = voiceChannel;
            _threadEvent = new AutoResetEvent(true);
            _thread = Task.Run(ThreadEntry);

            _mixer = new MultiChannelMixer();
        }

        public void AddRange([NotNull] IEnumerable<IChannel> sources)
        {
            foreach (var source in sources)
                Add(source);
        }

        public void Add([NotNull] IChannel source)
        {
            _mixer.Add(source);
        }

        public void Remove([NotNull] IChannel source)
        {
            _mixer.Remove(source);
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
                        var playing = _mixer.IsPlaying;

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
                        await WriteOutput(_mixer, s, _buffer.Length, _buffer);
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
    }
}
