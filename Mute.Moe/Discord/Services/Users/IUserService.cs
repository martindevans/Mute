using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Discord.Services.Users
{
    internal interface IUserService
    {
        public Task<IUser?> GetUser(ulong id, IGuild? guild = null);
    }
}
