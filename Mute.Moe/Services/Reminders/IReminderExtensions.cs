using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Reminders;

/// <summary>
/// Extensions related to reminders
/// </summary>
public static class IReminderExtensions
{
    /// <summary>
    /// Given a string, attempt to extract a datetime for when it should be sent as a reminder
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static DateTime? TryParseReminderMoment(this string message)
    {
        // Parse a moment from message
        var exactTimeResult = FuzzyParsing.Moment(message);
        if (exactTimeResult.IsValid)
            return exactTimeResult.Value;

        // Attempt to parse a time range instead of an exact time (e.g. `next week`)
        var rangeTimeResult = FuzzyParsing.MomentRange(message);
        if (!rangeTimeResult.IsValid)
            return null;

        // Send the reminder just after the start of the range
        var (start, end) = rangeTimeResult.Value;
        var duration = (end - start).Duration();

        // If it's a short duration, remind as soon as it starts
        if (duration < TimeSpan.FromDays(1))
            return start;

        // If it's a midnight start time, add on 6 hours to move the reminder into the early hours of the day
        if (start.Hour == 0)
            return start + TimeSpan.FromHours(6);

        return start;
    }
}