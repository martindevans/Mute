using System;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace Mute.Moe.Models
{
    [Serializable]
    public struct GuildInfo
    {
        public DateTime CreatedAtUtc { get; }
        public string IconUrl { get; }
        public string SplashUrl { get; }
        public ulong OwnerId { get; }
        public string Name { get; }
        public int Members { get; }
        public ulong Id { get; }

        public GuildInfo([NotNull] SocketGuild guild)
        {
            CreatedAtUtc = guild.CreatedAt.UtcDateTime;
            IconUrl = guild.IconUrl;
            Id = guild.Id;
            Members = guild.MemberCount;
            Name = guild.Name;
            OwnerId = guild.OwnerId;
            SplashUrl = guild.SplashUrl;
        }
    }
}
