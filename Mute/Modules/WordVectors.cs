using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Services;

namespace Mute.Modules
{
    [Group("word")]
    public class WordVectors
        : BaseModule
    {
        private readonly WordVectorsService _wordVectors;

        public WordVectors(WordVectorsService wordVectors)
        {
            _wordVectors = wordVectors;
        }

        [Command("vector")]
        public async Task GetWordVector(string word)
        {
            var vector = await _wordVectors.GetVector(word);
            if (vector == null)
            {
                await TypingReplyAsync("I don't know that word");
                return;
            }

            var arr = new StringBuilder(vector.Count * 4);
            arr.Append('[');
            for (var i = 0; i < vector.Count; i++)
            {
                if (i != 0)
                    arr.Append(", ");
                var num = vector[i] * 100;
                if (Math.Abs(num) < 1)
                    arr.Append('0');
                else
                    arr.Append(num.ToString("##"));
            }
            arr.Append(']');

            await ReplyAsync(arr.ToString());
        }

        [Command("similarity")]
        public async Task GetVectorSimilarity(string a, string b)
        {
            var result = await _wordVectors.CosineDistance(a, b);

            if (result < 0.2)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are very different ({result:0.0##})");
            }
            else if (result < 0.65)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are not very similar ({result:0.0##})");
            }
            else if (result < 0.85)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are quite similar ({result:0.0##})");
            }
            else
            {
                await TypingReplyAsync($"`{a}` and `{b}` are analagous ({result:0.0##})");
            }
        }

        [Command("cache-stats")]
        public async Task TestWvStats()
        {
            await ReplyAsync(new EmbedBuilder().AddField("Size", _wordVectors.CacheCount).AddField("Hits", _wordVectors.CacheHits).AddField("Miss", _wordVectors.CacheMisses));
        }
    }
}
