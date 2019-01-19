using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Moe.Extensions
{
    public static class SocketCommandContextExtensions
    {
        public static async Task<bool> HasPermission([NotNull] this SocketCommandContext context, ulong id)
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
