using System;

namespace Mute.Moe.Extensions
{
    public static class DateTimeExtensions
    {
        public static ulong UnixTimestamp(this DateTime time)
        {
            return (ulong)time.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
