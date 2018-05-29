using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using JetBrains.Annotations;
using Mute.Services.Audio.Clips;
using NAudio.Wave;

namespace Mute.Services.Audio
{
    internal class ThreadedAudioPlayer
    {
        private readonly AutoResetEvent _threadEvent;
        private readonly Task _thread;

        private volatile bool _stopped;
        private PlayingClip _playing;

        private int _skipRequest;

        [CanBeNull]
        public IAudioClip Playing => _playing?.Clip;

        public bool IsAlive => !_thread.IsCompleted;

        private readonly IVoiceChannel _channel;
        private readonly IClipProvider _provider;

        public ThreadedAudioPlayer(IVoiceChannel channel, IClipProvider provider)
        {
            _channel = channel;

            _provider = provider;
            _threadEvent = new AutoResetEvent(true);
            _thread = Task.Run(ThreadEntry);
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Ping()
        {
            _threadEvent.Set();
        }

        public void Stop()
        {
            _stopped = true;
            _threadEvent.Set();

            while (IsAlive)
                Thread.Sleep(1);
        }

        private async Task ThreadEntry()
        {
            try
            {
                using (var c = await _channel.ConnectAsync())
                using (var s = c.CreatePCMStream(AudioApplication.Mixed, _channel.Bitrate, 200))
                {
                    while (!_stopped)
                    {
                        //Sleep thread until something happens
                        _threadEvent.WaitOne(250);
                        if (_stopped)
                            return;

                        if (_playing != null)
                            await c.SetSpeakingAsync(true);

                        //Pump the currently playing audio for it's full duration
                        while (_playing != null)
                        {
                            await _playing.Update(s);
                            if (_playing.IsComplete)
                            {
                                _playing.Dispose();
                                _playing = null;

                                await c.SetSpeakingAsync(false);
                            }

                            //Sleep thread for a millisecond
                            _threadEvent.WaitOne(1);

                            //Immediately quit if the stop flag has been set
                            if (_stopped)
                                return;

                            //Check if we need to terminate playback of this clip
                            if (Interlocked.CompareExchange(ref _skipRequest, 0, 1) == 1 && _playing != null)
                            {
                                _playing.Dispose();
                                _playing = null;
                            }
                        }

                        //Try to find a track to play
                        var next = _provider.GetNextClip();
                        if (next != null)
                            _playing = new PlayingClip(next);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Skip()
        {
            if (Playing != null)
                Interlocked.Exchange(ref _skipRequest, 1);
        }

        private class PlayingClip
            : IDisposable
        {
            public IAudioClip Clip { get; }
        
            public bool IsComplete { get; private set; }

            private bool _started;

            private MediaFoundationResampler _resampler;
            private IDisposable _source;
            private byte[] _buffer;

            private DateTime? _firstStartAttemptUtc;

            public PlayingClip(IAudioClip clip)
            {
                Clip = clip;
            }

            public async Task Update([NotNull] Stream stream)
            {
                if (!_started)
                {
                    Start();
                    return;
                }

                if (IsComplete)
                    return;

                await Pump(stream);
            }

            private async Task Pump([NotNull] Stream stream)
            {
                var blockSize = _buffer.Length;
                var byteCount = _resampler.Read(_buffer, 0, blockSize);

                // Check if the frame is incomplete
                if (byteCount < blockSize)
                {
                    // Zero out the remaining space
                    for (var i = byteCount; i < blockSize; i++)
                        _buffer[i] = 0;
                    IsComplete = true;
                }

                // Send data
                await stream.WriteAsync(_buffer, 0, blockSize);
            }

            private void Start()
            {
                //Store when we first start trying to start
                if (!_firstStartAttemptUtc.HasValue)
                    _firstStartAttemptUtc = DateTime.UtcNow;

                //Check for 30s timeout
                if (DateTime.UtcNow - _firstStartAttemptUtc.Value > TimeSpan.FromSeconds(30))
                    IsComplete = true;

                //Exit out if the clip isn't ready yet
                if (!Clip.IsLoaded)
                    return;

                var format = new WaveFormat(48000, 16, 2);
                
                var s = Clip.Open();
                _source = s as IDisposable;

                _resampler = new MediaFoundationResampler(s.ToWaveProvider(), format) {
                    ResamplerQuality = 60
                };

                // Copy the data in ~200ms blocks
                var blockSize = format.AverageBytesPerSecond / 50;
                _buffer = new byte[blockSize];

                _started = true;
            }

            public void Dispose()
            {
                _source?.Dispose();
                _source = null;

                _resampler?.Dispose();
                _resampler = null;
            }
        }
    }
}
