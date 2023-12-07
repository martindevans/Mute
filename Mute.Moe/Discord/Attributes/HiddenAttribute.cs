namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Hide a command from help
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
internal class HiddenAttribute
    : Attribute;