using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Rtrieve information about anime characters
/// </summary>
public interface ICharacterInfo
{
    Task<ICharacter?> GetCharacterInfoAsync(string search);

    IAsyncEnumerable<ICharacter> GetCharactersInfoAsync(string search);
}

/// <summary>
/// A character in a anime or manga
/// </summary>
public interface ICharacter
{
    /// <summary>
    /// Unique ID for this character
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Given name for this character
    /// </summary>
    string GivenName { get; }

    /// <summary>
    /// Family name for this character
    /// </summary>
    string FamilyName { get; }

    /// <summary>
    /// General description of this character
    /// </summary>
    string Description { get; }

    /// <summary>
    /// URL linking to a web page about this character
    /// </summary>
    string Url { get; }

    /// <summary>
    /// URL for an image of this character
    /// </summary>
    string ImageUrl { get; }
}