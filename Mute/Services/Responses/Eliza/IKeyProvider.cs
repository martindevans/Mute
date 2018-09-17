using System.Collections.Generic;
using JetBrains.Annotations;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Services.Responses.Eliza
{
    public interface IKeyProvider
    {
        /// <summary>
        /// Provides a new set of basic rules for the conversation engine
        /// </summary>
        [NotNull, ItemNotNull]
        IEnumerable<Key> Keys { get; }
    }
}
