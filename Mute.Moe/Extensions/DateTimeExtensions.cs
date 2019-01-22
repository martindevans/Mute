using System;

namespace Mute.Moe.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        public static ulong UnixTimestamp(this DateTime time)
        {
            return (ulong)time.Subtract(UnixEpoch).TotalSeconds;
        }

        public static DateTime FromUnixTimestamp(this ulong unixTime)
        {
            return UnixEpoch.Add(TimeSpan.FromSeconds(unixTime));
        }
    }
}
