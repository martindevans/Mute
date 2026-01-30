using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Mute.Moe.Services.Audio.Mixing;
using NAudio.Wave;
using Serilog;

namespace Mute.Moe.Services.Audio;

/// <summary>
/// Pumps audio from a mixer to a guild voice channel
/// </summary>
public class ThreadedGuildVoice
    : IGuildVoice
{
    /// <summary>
    /// The guild this pump is for
    /// </summary>
    public IGuild Guild { get; }

    /// <summary>
    /// The channel this pump is for
    /// </summary>
    public IVoiceChannel? Channel => _pump?.Channel;

    private readonly MultiChannelMixer _mixer = new();
    private readonly DiscordSocketClient _client;

    private AudioPump? _pump;

    /// <summary>
    /// Create a new pump for the guild
    /// </summary>
    /// <param name="guild"></param>
    /// <param name="client"></param>
    public ThreadedGuildVoice(IGuild guild, DiscordSocketClient client)
    {
        Guild = guild;
        _client = client;

        client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
    {
        // Ignore this event if the bot isn't in a channel
        if (Channel == null)
            return;

        // Ignore this event if it is the bot itself doing something
        if (user.Id == _client.CurrentUser.Id)
            return;

        // Ignore this event if user is in another channel
        if (before.VoiceChannel != Channel)
            return;

        // If there are other users in the channel stay in the channel
        var count = await Channel.GetUsersAsync().Select(a => a.Count).SumAsync();
        if (count > 1)
            return;

        // No one is listening :(
        await Stop();
    }

    /// <inheritdoc />
    public Task Stop()
    {
        return Move(null);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

        public Task Stop()
        {
            _cancellation.Cancel();

            return _thread;
        }

        private async Task ThreadEntry()
        {
            var audioCopyBuffer = new byte[_mixer.WaveFormat.AverageBytesPerSecond / 10];

            try
            {
                using var c = await Channel.ConnectAsync();

                await using var s = c.CreatePCMStream(AudioApplication.Mixed, Channel.Bitrate);

                var speakingState = false;
                while (!_cancellation.IsCancellationRequested)
                {
                    // Wait for an event to happen to wake up the thread
                    if (!speakingState)
                    {
                        _cancellation.Token.WaitHandle.WaitOne(250);
                        await c.SetSpeakingAsync(speakingState);
                    }

                    // Break out if stop flag has been set
                    if (_cancellation.IsCancellationRequested)
                        return;

                    // Count up how many channels are playing.
                    var playing = _mixer.IsPlaying;

                    // Set playback state if it has changed
                    if (playing != speakingState)
                    {
                        speakingState = playing;
                        await c.SetSpeakingAsync(speakingState);
                    }

                    // Early exit if nothing is playing
                    if (!speakingState)
                        continue;

                    // Copy mixed audio to the output
                    await WriteOutput(_mixer, s, audioCopyBuffer.Length, audioCopyBuffer);
                }

                await c.SetSpeakingAsync(false);
                await c.StopAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, $"Exception killed {nameof(ThreadedGuildVoice)} thread");
                throw;
            }

            return;

            static async Task WriteOutput(IWaveProvider waveSource, Stream waveSink, int sampleCount, byte[] buffer)
            {
                while (sampleCount > 0)
                {
                    // Read output from mixer
                    var mixed = waveSource.Read(buffer, 0, buffer.Length);
                    sampleCount -= mixed;

                    // Send the mixed audio buffer to discord
                    await waveSink.WriteAsync(buffer.AsMemory(0, mixed));
                
                    // If no audio was mixed early exit, this probably indicates the end of the stream
                    if (mixed == 0)
                    {
                        await waveSink.FlushAsync();
                        return;
                    }
                }
            }
        }
    }
}