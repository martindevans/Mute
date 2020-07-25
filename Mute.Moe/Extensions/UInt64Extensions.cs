using BalderHash.Extensions;

namespace Mute.Moe.Extensions
{
    public static class UInt64Extensions
    {
         public static string MeaninglessString(this ulong number)
        {
            return number.BalderHash();
        }
    }
}
