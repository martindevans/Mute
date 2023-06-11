using System;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Group for all commands in this module to be shown in help
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class HelpGroupAttribute
    : Attribute
{
    public string GroupId { get; }

    public HelpGroupAttribute(string groupId)
    {
        GroupId = groupId;
    }
}