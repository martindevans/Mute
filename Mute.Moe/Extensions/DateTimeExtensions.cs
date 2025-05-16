namespace Mute.Moe.Extensions;

public static class DateTimeExtensions
{
    public static ulong UnixTimestamp(this DateTime time)
    {
        return (ulong)time.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }

    public static DateTime FromUnixTimestamp(this ulong unixTime)
    {
        var t = DateTime.UnixEpoch.Add(TimeSpan.FromSeconds(unixTime));
        return new DateTime(t.Ticks, DateTimeKind.Utc);
    }
}