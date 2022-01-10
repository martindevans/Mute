using System;
using System.Collections.Generic;
using System.IO;

namespace Mute.Moe.Services.Words
{
    public class WordsService
    {
        private readonly HashSet<string> _words;

        public IEnumerable<string> Words => _words;

        public WordsService(Configuration config)
        {
            if (config.Dictionary == null)
                throw new ArgumentNullException(nameof(config.Dictionary));
            if (config.Dictionary.WordListPath == null)
                throw new ArgumentNullException(nameof(config.Dictionary.WordListPath));

            _words = new HashSet<string>(File.ReadAllLines(config.Dictionary.WordListPath));
        }

        public bool Contains(string word)
        {
            return _words.Contains(word.ToLowerInvariant());
        }
    }
}
