using System.Threading.Tasks;
using Discord;


namespace Mute.Moe.Services.Groups;

/// <summary>
/// Management of user groups
/// </summary>
public interface IGroups
{
    /// <summary>
    /// Check if the given group has been "unlocked", which allows anyone to join it
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    Task<bool> IsUnlocked(IRole role);

    /// <summary>
    /// Get all unlocked groups in the guild
    /// </summary>
    /// <param name="guild"></param>
    /// <returns></returns>
    IAsyncEnumerable<IRole> GetUnlocked(IGuild guild);

    /// <summary>
    /// Unlock a group
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    Task Unlock(IRole role);

    /// <summary>
    /// Lock a group
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    Task Lock(IRole role);
}