using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Services.Audio.Mixing;

namespace Mute.Moe.Discord.Services.Audio.Playback
{
    public class MultichannelAudioService
    {
        private readonly DiscordSocketClient _client;

        private readonly List<(string, IChannel)> _channels = new List<(string, IChannel)>();

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
                return;

            //Ignore this event if it is the bot itself doing something
            if (user.Id == _client.CurrentUser.Id)
                return;

            //Ignore this event if it's not someone leaving the channel
            if (before.VoiceChannel != Channel || after.VoiceChannel != null)
                return;

            //If there are other users in the channel stay in the channel
            var count = await Channel.GetUsersAsync().Select(a => a.Count).Sum();
            if (count > 1)
                return;

            //No one is listening :(
            await Stop();
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
            _player = new MultichannelAudioPlayer(channel);
            _player.AddRange(_channels.Select(a => a.Item2));
        }

        public IChannel GetOrOpen<T>(string name, Func<T> open)
            where T : IChannel
        {
            var item = _channels.Find(a => a.Item1 == name);
            if (item != default)
                return item.Item2;

            var c = open();
            _channels.Add((name, c));
            return c;
        }

        public void Open(IChannel channel)
        {
            _channels.Add((Guid.NewGuid().ToString(), channel));
            _player?.Add(channel);
        }

        public void Close(IChannel channel)
        {
            _channels.RemoveAll(a => a.Item2 == channel);
            _player?.Remove(channel);
        }

        public async Task Stop()
        {
            if (_player != null)
                await _player.Stop();
            _player = null;
            Channel = null;

            foreach (var channel in _channels)
                channel.Item2.Stop();
        }
    }

    public interface IChannel
        : IMixerInput
    {
        void Stop();
    }
}
