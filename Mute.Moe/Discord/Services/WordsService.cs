using System.Collections.Generic;
using System.IO;


namespace Mute.Moe.Discord.Services
{
    public class WordsService
    {
        private readonly HashSet<string> _words;

        public IEnumerable<string> Words => _words;

        public WordsService( Configuration config)
        {
            _words = new HashSet<string>(File.ReadAllLines(config.Dictionary.WordListPath));
        }

        public bool Contains( string word)
        {
            return _words.Contains(word.ToLowerInvariant());
        }
    }
}
