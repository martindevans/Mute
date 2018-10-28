using System;
using System.Collections.Generic;
using System.Linq;

namespace Mute.Services
{
    public class TimeService
    {
        private class MiliaryTz
        {
            public readonly string Name;
            public readonly char Letter;
            public readonly int Offset;

            public MiliaryTz(string name, char letter, int offset)
            {
                Name = name;
                Letter = letter;
                Offset = offset;
            }
        }

        private static readonly IReadOnlyList<MiliaryTz> Timezones = new List<MiliaryTz> {
            new MiliaryTz("Alfa", 'A', 1),
            new MiliaryTz("Bravo", 'B', 2),
            new MiliaryTz("Charlie", 'C', 3),
            new MiliaryTz("Delta", 'D', 4),
            new MiliaryTz("Echo", 'E', 5),
            new MiliaryTz("Foxtrot", 'F', 6),
            new MiliaryTz("Golf", 'G', 7),
            new MiliaryTz("Hotel", 'H', 8),
            new MiliaryTz("India", 'I', 9),
            new MiliaryTz("Kilo", 'K', 10),
            new MiliaryTz("Lima", 'L', 11),
            new MiliaryTz("Mike", 'M', 12),
            new MiliaryTz("November", 'N', -1),
            new MiliaryTz("Oscar", 'O', -2),
            new MiliaryTz("Papa", 'P', -3),
            new MiliaryTz("Quebec", 'Q', -4),
            new MiliaryTz("Romeo", 'R', -5),
            new MiliaryTz("Sierra", 'S', -6),
            new MiliaryTz("Tango", 'T', -7),
            new MiliaryTz("Uniform", 'U', -8),
            new MiliaryTz("Victor", 'V', -9),
            new MiliaryTz("Whiskey", 'W', -10),
            new MiliaryTz("X-ray", 'X', -11),
            new MiliaryTz("Yankee", 'Y', -12),
            new MiliaryTz("Zulu", 'Z', 0),
        };

        private static readonly Dictionary<string, char> Abbreviations = new Dictionary<string, char> {
            { "UTC", 'Z' },
            { "GMT", 'Z' },
            { "BST", 'A' },
            { "CET", 'A' },
            { "CEST", 'A' },
            { "EST", 'R' },
            { "EDT", 'Q' },
            { "CST", 'S' },
            { "CDT", 'R' },
            { "PST", 'U' },
            { "PDT", 'T' },
            { "HKT", 'H' },
            { "HKST", 'I' },
            { "JST", 'I' }
        };

        public DateTime? TimeNow(string tzid)
        {
            var tz = Timezones.SingleOrDefault(t => t.Name.Equals(tzid, StringComparison.InvariantCultureIgnoreCase) || t.Letter.ToString().Equals(tzid, StringComparison.InvariantCultureIgnoreCase));
            if (tz == null)
            {
                if (Abbreviations.TryGetValue(tzid.ToUpperInvariant(), out var code))
                    tz = Timezones.SingleOrDefault(t => t.Letter.Equals(code));
            }

            if (tz == null)
                return null;

            return DateTime.UtcNow.AddHours(tz.Offset);
        }
    }
}
