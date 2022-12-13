using System;
using System.Linq;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Reminders;

public interface IReminders
{
    /// <summary>
    /// Create a new reminder
    /// </summary>
    /// <returns></returns>
    Task<IReminder> Create(DateTime triggerTime, string prelude, string msg, ulong channelId, ulong userId);

    /// <summary>
    /// Get all reminders in date order filtered by user, time range, channel or status and limited by a max count
    /// </summary>
    /// <returns></returns>
    IOrderedAsyncEnumerable<IReminder> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null);

    /// <summary>
    /// Delete a reminder
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> Delete(ulong userId, uint id);

    /// <summary>
    /// Action invoked when a new reminder is created
    /// </summary>
    event Action<IReminder> ReminderCreated;

    /// <summary>
    /// Action invoked when a reminder is deleted
    /// </summary>
    event Action<uint> ReminderDeleted;
}

public interface IReminder
{
    uint ID { get; }
    ulong UserId { get; }
    ulong ChannelId { get; }

    string? Prelude { get; }
    string Message { get; }

    DateTime TriggerTime { get; }
}