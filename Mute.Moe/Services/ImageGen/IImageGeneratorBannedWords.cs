namespace Mute.Moe.Services.ImageGen
{
    public interface IImageGeneratorBannedWords
    {
        bool IsBanned(string prompt);
    }

    public class HardcodedBannedWords
        : IImageGeneratorBannedWords
    {
        private static readonly string[] _bannedWords =
        {
            "nsfw", "porn", "erotic", "fuck", "naked", "nude",
            "spider", "arachnid", "tarantula",
        };

        public bool IsBanned(string prompt)
        {
            return _bannedWords.Any(word => prompt.Contains(word, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
