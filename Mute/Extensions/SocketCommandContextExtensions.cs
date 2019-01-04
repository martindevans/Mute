using System;
using Discord.Commands;
using Discord;
using System.Threading.Tasks;
using System.Linq;

namespace Mute.Extensions
{
    public static class SocketCommandContextExtensions
    {
        public static async Task<bool> HasPermission(this SocketCommandContext context, ulong id)
        {
            if (!(context.Channel is IGuildChannel gc))
                return false;

            var gu = (context.User as IGuildUser) ?? await gc.GetUserAsync(context.User.Id);
            if (gu == null)
                return false;

            return gu.RoleIds.Contains(id);
        }
    }
}
