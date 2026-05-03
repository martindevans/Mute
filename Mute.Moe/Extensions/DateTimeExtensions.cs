using Mute.Anilist;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions to <see cref="DateTime"/>
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Extensions to <see cref="DateTime"/>
    /// </summary>
    extension(DateTime time)
    {
        /// <summary>
        /// Convert given datetime to a unix timestamp
        /// </summary>
        /// <returns></returns>
        public ulong UnixTimestamp()
        {
            return (ulong)time.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Get the media season this date is in
        /// </summary>
        /// <returns></returns>
        public MediaSeason MediaSeason()
        {
                var month = DateTime.Now.Month;

                return month switch
                {
                    >= 0 and <= 2 => Anilist.MediaSeason.Winter,
                    >= 3 and <= 5 => Anilist.MediaSeason.Spring,
                    >= 6 and <= 8 => Anilist.MediaSeason.Summer,
                    _ => Anilist.MediaSeason.Fall,
                };
        }
    }

    /// <summary>
    /// Extensions to ulong, interpreting it as a unix timestamp
    /// </summary>
    /// <param name="unixTime"></param>
    extension(ulong unixTime)
    {
        /// <summary>
        /// Convert unix timestamp back to a datetime
        /// </summary>
        /// <returns></returns>
        public DateTime FromUnixTimestamp()
        {
            var t = DateTime.UnixEpoch.Add(TimeSpan.FromSeconds(unixTime));
            return new DateTime(t.Ticks, DateTimeKind.Utc);
        }
    }
}