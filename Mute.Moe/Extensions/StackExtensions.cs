using System.Collections.Generic;


namespace Mute.Moe.Extensions;

public static class StackExtensions
{
    public static T? PopOrDefault<T>(this Stack<T> stack)
        where T : class
    {
        if (stack.Count == 0)
            return default;

        return stack.Pop();
    }
}