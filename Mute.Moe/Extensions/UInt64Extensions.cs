using System;


namespace Mute.Moe.Extensions
{
    public struct FriendlyId64
    {
        private const ulong Offset = 0xe106c179ac47eead;

        private const ulong Multiply = 3481248731582150605;
        private const ulong MultiplyInverse = 3333;

        public ulong Value { get; }

        public FriendlyId64(ulong value)
        {
            Value = value;
        }

        public static FriendlyId64? Parse( string str)
        {
            var parts = str.Split('-');
            if (parts.Length != 4)
                return null;

            var ab = parts[0];
            if (ab.Length != 6)
                return null;
            var cd = parts[1];
            if (cd.Length != 6)
                return null;
            var ef = parts[2];
            if (ef.Length != 6)
                return null;
            var gh = parts[3];
            if (gh.Length != 6)
                return null;

            var abcd = FriendlyId32.Parse($"{ab}-{cd}");
            var efgh = FriendlyId32.Parse($"{ef}-{gh}");

            if (!abcd.HasValue || !efgh.HasValue)
                return null;

            var value = ((ulong)abcd.Value.Value << 32) | efgh.Value.Value;
            unchecked
            {
                value *= MultiplyInverse;
                value -= Offset;
            }

            return new FriendlyId64(value);
        }

         public override string ToString()
        {
            var number = Value;
            unchecked
            {
                number += Offset;
                number *= Multiply;
            }

            unsafe
            {
                var ints = new Span<uint>(&number, 2);

                var n1 = ints[0];
                var n2 = ints[1];

                    var abcd = new FriendlyId32(n1);
                var efgh = new FriendlyId32(n2);

                return $"{efgh}-{abcd}";
            }
        }
    }

    public static class UInt64Extensions
    {
         public static string MeaninglessString(this ulong number)
        {
            return new FriendlyId64(number).ToString();
        }
    }
}
