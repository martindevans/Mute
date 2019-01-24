using System;

namespace Mute.Moe.Discord.Attributes
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
