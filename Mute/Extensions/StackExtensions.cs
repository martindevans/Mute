using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class StackExtensions
    {
        public static T PopOrDefault<T>([NotNull] this Stack<T> stack)
        {
            if (stack.Count == 0)
                return default;

            return stack.Pop();
        }
    }
}
