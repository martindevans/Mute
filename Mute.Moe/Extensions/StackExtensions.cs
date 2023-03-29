using System.Collections.Generic;


namespace Mute.Moe.Extensions;

public static class StackExtensions
{
    public static T? PopOrDefault<T>(this Stack<T> stack)
        where T : class
    {
        stack.TryPop(out var value);
        return value;
    }
}