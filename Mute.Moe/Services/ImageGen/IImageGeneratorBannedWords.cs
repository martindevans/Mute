namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Indicates if a prompt for image generation is allowed
/// </summary>
public interface IImageGeneratorBannedWords
{
    /// <summary>
    /// Check the given prompt
    /// </summary>
    /// <param name="prompt"></param>
    /// <returns></returns>
    bool IsBanned(string prompt);
}

/// <inheritdoc />
public class HardcodedBannedWords
    : IImageGeneratorBannedWords
{
    private static readonly string[] _bannedWords =
    [
        "nsfw", "porn", "erotic", "fuck", "naked", "nude", "hentai", "tits", "sex", "penis",
        "spider", "arachnid", "tarantula", "arachnophobia",
    ];

    /// <inheritdoc />
    public bool IsBanned(string prompt)
    {
        return _bannedWords.Any(word => prompt.Contains(word, StringComparison.InvariantCultureIgnoreCase));
    }
}