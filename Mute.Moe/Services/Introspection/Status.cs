using System;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Introspection.Uptime;

namespace Mute.Moe.Services.Introspection
{
    public class Status
    {
        private readonly IUptime _uptime;
        public TimeSpan Uptime => _uptime.Uptime;

        private readonly DiscordSocketClient _client;
        public TimeSpan Latency => TimeSpan.FromMilliseconds(_client.Latency);
        public int Shard => _client.ShardId;
        public IReadOnlyCollection<IGuild> Guilds => _client.Guilds;

        public long MemoryWorkingSet => Environment.WorkingSet;
        public long TotalGCMemory => GC.GetTotalMemory(false);

        public Status(IUptime uptime, DiscordSocketClient client)
        {
            _uptime = uptime;
            _client = client;
        }
    }
}
