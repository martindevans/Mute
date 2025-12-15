using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Discord.Services.Users;

/// <summary>
/// Fetch user related info
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get the user with the given ID, optionally in a specific guild
    /// </summary>
    /// <param name="id"></param>
    /// <param name="guild"></param>
    /// <returns></returns>
    public Task<IUser?> GetUser(ulong id, IGuild? guild = null);
}

/// <summary>
/// Extensions to <see cref="IUserService"/>
/// </summary>
public static class IUserServiceExtensions
{
    /// <summary>
    /// Get the name of a user from their ID
    /// </summary>
    /// <param name="uservice"></param>
    /// <param name="id"></param>
    /// <param name="guild"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public static async Task<string> Name(this IUserService uservice, ulong id, IGuild? guild = null, bool mention = false)
    {
        var user = await uservice.GetUser(id, guild);

        return user == null
            ? $"UNK:{id}"
            : uservice.Name(user, mention);
    }

    /// <summary>
    /// Get the name of a user
    /// </summary>
    /// <param name="_"></param>
    /// <param name="user"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public static string Name(this IUserService _, IUser user, bool mention = false)
    {
        if (mention)
            return user.Mention;

        var name = (user as IGuildUser)?.Nickname ?? user.GlobalName ?? user.Username;

        // Sanitise formatting
        return name
              .Replace("*", "\\*")
              .Replace("_", "\\_");
    }
}