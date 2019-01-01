using System;

namespace Mute
{
    /// <summary>
    /// Hide a command from help
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class HiddenAttribute
        : Attribute
    {
    }
}
