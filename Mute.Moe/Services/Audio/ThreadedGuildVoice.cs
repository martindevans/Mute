using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Mute.Moe.Services.Audio.Mixing;
using Mute.Moe.Services.Audio.Mixing.Channels;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio;

public class ThreadedGuildVoice
    : IGuildVoice
{
    public IGuild Guild { get; }

    private AudioPump? _pump;
    public IVoiceChannel? Channel => _pump?.Channel;

    private readonly MultiChannelMixer _mixer = new();
    private readonly DiscordSocketClient _client;

    public ThreadedGuildVoice(IGuild guild, DiscordSocketClient client)
    {
        Guild = guild;
        _client = client;

        client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        //Ignore this event if the bot isn't in a channel
        if (Channel == null)
            return;

        //Ignore this event if it is the bot itself doing something
        if (user.Id == _client.CurrentUser.Id)
            return;

        //Ignore this event if user is in another channel
        if (before.VoiceChannel != Channel)
            return;

        //If there are other users in the channel stay in the channel
        var count = await Channel.GetUsersAsync().Select(a => a.Count).SumAsync();
        if (count > 1)
            return;

        //No one is listening :(
        await Stop();
    }

    public async Task Stop()
    {
        await Move(null);
    }

    public async Task Move(IVoiceChannel? channel)
    {
        if (Channel?.Id == channel?.Id)
            return;

        // Close current pump
        if (_pump != null)
            await _pump.Stop();

        _pump = null;

        // Open new channel
        if (channel != null)
            _pump = new AudioPump(channel, _mixer);
    }

    public void Open(IMixerChannel channel)
    {
        _mixer.Add(channel);
    }

    private class AudioPump
    {
        private readonly MultiChannelMixer _mixer;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _thread;

        public IVoiceChannel Channel { get; }

        public AudioPump(IVoiceChannel channel, MultiChannelMixer mixer)
        {
            Channel = channel;

            _mixer = mixer;
            _thread = Task.Run(ThreadEntry, _cancellation.Token);
        }

        public async Task Stop()
        {
            _cancellation.Cancel();

            await _thread;
        }

        private async Task ThreadEntry()
        {
            static async Task WriteOutput(IWaveProvider waveSource, Stream waveSink, int sampleCount, byte[] buffer)
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
                    {
                        await waveSink.FlushAsync();
                        return;
                    }
                }
            }

            var audioCopyBuffer = new byte[_mixer.WaveFormat.AverageBytesPerSecond / 10];

            try
            {
                using var c = await Channel.ConnectAsync();

                await using var s = c.CreatePCMStream(AudioApplication.Mixed, Channel.Bitrate);

                var speakingState = false;
                while (!_cancellation.IsCancellationRequested)
                {
                    //Wait for an event to happen to wake up the thread
                    if (!speakingState)
                    {
                        _cancellation.Token.WaitHandle.WaitOne(250);
                        await c.SetSpeakingAsync(speakingState);
                    }

                    //Break out if stop flag has been set
                    if (_cancellation.IsCancellationRequested)
                        return;

                    //Count up how many channels are playing.
                    var playing = _mixer.IsPlaying;

                    //Set playback state if it has changed
                    if (playing != speakingState)
                    {
                        speakingState = playing;
                        await c.SetSpeakingAsync(speakingState);
                    }

                    //Early exit if nothing is playing
                    if (!speakingState)
                        continue;

                    //Copy mixed audio to the output
                    await WriteOutput(_mixer, s, audioCopyBuffer.Length, audioCopyBuffer);
                }

                await c.SetSpeakingAsync(false);
                await c.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}