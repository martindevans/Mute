using System.Threading.Tasks;

namespace Mute.Moe.Services.Archive;

/// <summary>
/// Stores potential "holes" in the chat archive that might need to be filled in
/// </summary>
public interface IChatArchiveHoles
{
    /// <summary>
    /// Create a potential hole, starting at the given message and forwards or backwards in time
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="startMessage"></param>
    /// <param name="forward"></param>
    /// <returns></returns>
    Task Create(ulong channel, ulong startMessage, bool forward);

    /// <summary>
    /// Get a random hole
    /// </summary>
    /// <returns></returns>
    Task<ChatArchiveHole?> Read();

    /// <summary>
    /// Delete the hole with the given ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task Delete(long id);

    /// <summary>
    /// Get all "holes" in the database, optionally filtered by channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    Task<IReadOnlyList<ChatArchiveHole>> List(ulong? channel);

    /// <summary>
    /// Get the number of "holes" in the database. Optionally filtered by channel
    /// </summary>
    /// <returns></returns>
    int Count(ulong? channel);
}

/// <summary>
/// a potential "hole" in the message archive
/// </summary>
/// <param name="StartMessageId"></param>
/// <param name="Forward"></param>
public record ChatArchiveHole(long Id, ulong ChannelId, ulong StartMessageId, bool Forward);