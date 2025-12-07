using System.Threading.Tasks;

namespace Mute.Moe.Services.Reminders;

/// <summary>
/// Service for storing and retrieving reminders
/// </summary>
public interface IReminders
{
    /// <summary>
    /// Create a new reminder
    /// </summary>
    /// <returns></returns>
    Task<Reminder> Create(DateTime triggerTime, string prelude, string msg, ulong channelId, ulong userId);

    /// <summary>
    /// Get all reminders in date order filtered by user, time range, channel or status and limited by a max count
    /// </summary>
    /// <returns></returns>
    IOrderedAsyncEnumerable<Reminder> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null);

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
    event Action<Reminder> ReminderCreated;

    /// <summary>
    /// Action invoked when a reminder is deleted
    /// </summary>
    event Action<uint> ReminderDeleted;
}

/// <summary>
/// A reminder
/// </summary>
/// <param name="ID"></param>
/// <param name="UserId"></param>
/// <param name="ChannelId"></param>
/// <param name="Prelude"></param>
/// <param name="Message"></param>
/// <param name="TriggerTime"></param>
public sealed record Reminder(uint ID, DateTime TriggerTime, string? Prelude, string Message, ulong ChannelId, ulong UserId)
    : IComparable<Reminder>
{
    /// <inheritdoc />
    public int CompareTo(Reminder? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (other is null)
            return 1;

        // ReSharper disable once ImpureMethodCallOnReadonlyValueField
        return TriggerTime.CompareTo(other.TriggerTime);
    }

    #region equality
    /// <inheritdoc />
    public bool Equals(Reminder? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return ID == other.ID;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (int)ID;
    }
    #endregion
}