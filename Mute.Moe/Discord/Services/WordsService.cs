using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Services
{
    public class WordsService
    {
        private readonly IReadOnlyList<string> _words;
        public IEnumerable<string> Words => _words;

        public WordsService([NotNull] Configuration config)
        {
            _words = File.ReadAllLines(config.Dictionary.WordListPath);
        }

        public bool Contains([NotNull] string responseContent)
        {
            return _words.Contains(responseContent.ToLowerInvariant());
        }
    }
}
