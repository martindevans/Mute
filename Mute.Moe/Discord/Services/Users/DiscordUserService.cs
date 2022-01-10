using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FluidCaching;

namespace Mute.Moe.Discord.Services.Users
{
    internal class DiscordUserService
        : IUserService
    {
        private readonly DiscordSocketClient _client;
        
        private readonly IIndex<ulong, IUser> _itemById;
        private readonly IIndex<(ulong, ulong), IGuildUser> _itemByGuildAndId;

        public DiscordUserService(DiscordSocketClient client)
        {
            _client = client;
            var cacheUsers = new FluidCache<IUser>(1024, TimeSpan.FromHours(1), TimeSpan.FromDays(7), () => DateTime.UtcNow);
            _itemById = cacheUsers.AddIndex("id", a => a.Id);

            var cacheGuildUsers = new FluidCache<IGuildUser>(1024, TimeSpan.FromHours(1), TimeSpan.FromDays(7), () => DateTime.UtcNow);
            _itemByGuildAndId = cacheGuildUsers.AddIndex("guildAndId", a => (a.GuildId, a.Id));
        }

        public async Task<IUser?> GetUser(ulong id, IGuild? guild = null)
        {
            if (guild != null)
            {
                var gu = await _itemByGuildAndId.GetItem((guild.Id, id), _ => GetUncached(id, guild));
                if (gu != null)
                    return gu;
            }

            return await _itemById.GetItem(id, GetUncached);
        }

        private static Task<IGuildUser> GetUncached(ulong user, IGuild guild)
        {
            return guild.GetUserAsync(user);
        }

        private async Task<IUser> GetUncached(ulong user)
        {
            return await _client.GetUserAsync(user)
                ?? await _client.Rest.GetUserAsync(user);
        }
    }
}
