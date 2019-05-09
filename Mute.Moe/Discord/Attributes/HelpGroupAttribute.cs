using System;

namespace Mute.Moe.Discord.Attributes
{
    public class HelpGroupAttribute
        : Attribute
    {
        public string GroupId { get; }

        public HelpGroupAttribute(string groupId)
        {
            GroupId = groupId;
        }
    }
}
