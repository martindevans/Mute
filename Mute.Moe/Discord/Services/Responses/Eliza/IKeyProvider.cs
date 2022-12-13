using System.Collections.Generic;

using Mute.Moe.Discord.Services.Responses.Eliza.Engine;

namespace Mute.Moe.Discord.Services.Responses.Eliza;

public interface IKeyProvider
{
    /// <summary>
    /// Provides a new set of basic rules for the conversation engine
    /// </summary>
    IEnumerable<Key> Keys { get; }
}