using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Anime;

public interface ICharacterInfo
{
    Task<ICharacter?> GetCharacterInfoAsync(string search);

    IAsyncEnumerable<ICharacter> GetCharactersInfoAsync(string search);
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