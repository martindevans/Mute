using BalderHash.Extensions;

namespace Mute.Moe.Extensions;

public static class UInt32Extensions
{
    public static string MeaninglessString(this uint number)
    {
        return number.BalderHash();
    }
}