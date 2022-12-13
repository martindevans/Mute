using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Discord.Services.Users;

public interface IUserService
{
    public Task<IUser?> GetUser(ulong id, IGuild? guild = null);
}

public static class IUserServiceExtensions
{
    public static async Task<string> Name(this IUserService uservice, ulong id, IGuild? guild = null, bool mention = false)
    {
        var user = await uservice.GetUser(id, guild);

        return user == null
            ? $"UNK:{id}"
            : uservice.Name(user, mention);
    }

    public static string Name(this IUserService uservice, IUser user, bool mention = false)
    {
        if (mention)
            return user.Mention;

        return (user as IGuildUser)?.Nickname ?? user.Username;
    }
}