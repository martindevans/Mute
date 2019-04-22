using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Anime
{
    public interface ICharacterInfo
    {
        [ItemCanBeNull] Task<ICharacter> GetCharacterInfoAsync(string search);

        [ItemCanBeNull] Task<IAsyncEnumerable<ICharacter>> GetCharactersInfoAsync(string search);
    }

    public interface ICharacter
    {
        string Id { get; }

        string GivenName { get; }

        string FamilyName { get; }

        string Description { get; }

        string Url { get; }

        string ImageUrl { get; }
    }
}
