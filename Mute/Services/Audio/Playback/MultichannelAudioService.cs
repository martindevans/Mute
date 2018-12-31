using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Services.Audio.Playback
{
    public class MultichannelAudioService
    {
        private readonly DiscordSocketClient _client;
        private readonly List<IChannel> _channels = new List<IChannel>();

        private MultichannelAudioPlayer _player;

        private IVoiceChannel _channel;
        [CanBeNull] public IVoiceChannel Channel => _channel;

        public MultichannelAudioService([NotNull] DiscordSocketClient client)
        {
            _client = client;
            client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        }

        private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            if (Channel == null)
                return;

            if (user.Id == _client.CurrentUser.Id)
                return;

            //Ensure we can get all users in this channel
            await _client.DownloadUsersAsync(new [] { Channel.Guild });

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
            if (_channel == channel)
                return;

            if (_player != null)
                await _player.Stop();

            _channel = channel;
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
            _channel = null;

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
