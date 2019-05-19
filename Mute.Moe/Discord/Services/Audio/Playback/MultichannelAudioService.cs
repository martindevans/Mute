using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Moe.Discord.Services.Audio.Playback
{
    public class MultichannelAudioService
    {
        private readonly DiscordSocketClient _client;
        private readonly List<IChannel> _channels = new List<IChannel>();

        private MultichannelAudioPlayer _player;

        [CanBeNull]
        public IVoiceChannel Channel { get; private set; }

        public MultichannelAudioService([NotNull] DiscordSocketClient client)
        {
            _client = client;
            client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        }

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            //Ignore this event if the bot isn't in a channel
            if (Channel == null)
            {
                Console.WriteLine("Ignoring voice event (bot not in channel)");
                return;
            }

            //Ignore this event if it is the bot itself doing something
            if (user.Id == _client.CurrentUser.Id)
            {
                Console.WriteLine("Ignoring voice event (event regards self)");
                return;
            }

            //Ignore this event if it's not someone leaving the channel
            if (before.VoiceChannel != Channel || after.VoiceChannel != null)
            {
                Console.WriteLine("Ignoring voice event (it's not a channel leave event)");
                return;
            }

            //If there are other users in the channel stay in the channel
            var count = await Channel.GetUsersAsync().Select(a => a.Count).Sum();
            if (count > 1)
            {
                Console.WriteLine($"Ignoring voice event (there are {count} users in channel)");
                return;
            }

            //No one is listening :(
            Console.WriteLine("Stopping voice");
            await Stop();
            Console.WriteLine("Stopped voice");
        }

        public async Task<bool> MoveChannel([CanBeNull] IUser user)
        {
            if (user is IVoiceState v && v.VoiceChannel != null)
            {
                await MoveChannel(v.VoiceChannel);
                return true;
            }
            else
                return false;
        }

        public async Task MoveChannel(IVoiceChannel channel)
        {
            if (Channel == channel)
                return;

            if (_player != null)
                await _player.Stop();

            Channel = channel;
            _player = new MultichannelAudioPlayer(channel, _channels);
        }

        public void Open(IChannel channel)
        {
            _channels.Add(channel);
            _player?.Add(channel);
        }

        public void Close(IChannel channel)
        {
            _channels.Remove(channel);
            _player?.Remove(channel);
        }

        public async Task Stop()
        {
            if (_player != null)
                await _player.Stop();
            _player = null;
            Channel = null;

            foreach (var channel in _channels)
                channel.Stop();
        }
    }

    public interface IChannel
        : ISampleProvider
    {
        bool IsPlaying { get; }

        void Stop();
    }
}
