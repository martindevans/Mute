using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Services;
using Mute.Moe.Services.Words;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    [Group("word")]
    public class Words
        : BaseModule
    {
        private readonly IWords _wordVectors;
        private readonly WordTrainingService _training;

        public Words(IWords wordVectors, WordTrainingService training)
        {
            _wordVectors = wordVectors;
            _training = training;
        }

        [Command("vector"), Summary("I will get the raw vector for a word")]
        public async Task GetWordVector(string word)
        {
            var vector = await _wordVectors.Vector(word);
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

        [Command("similarity"), Summary("I will tell you how similar two words are")]
        public async Task GetVectorSimilarity(string a, string b)
        {
            var result = await _wordVectors.Similarity(a, b);

            if (!result.HasValue)
            {
                var av = await _wordVectors.Vector(a);
                if (av == null)
                    await TypingReplyAsync("I don't know the word `{a}`");
                var bv = await _wordVectors.Vector(b);
                if (bv == null)
                    await TypingReplyAsync("I don't know the word `{b}`");
            }
            else if (result < 0.2)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are very different ({result:0.0##})");
            }
            else if (result < 0.5)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are not very similar ({result:0.0##})");
            }
            else if (result < 0.65)
            {
                await TypingReplyAsync($"`{a}` and `{b}` are a bit similar ({result:0.0##})");
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

        [Command("similar")]
        public async Task GetSimilarWords(string a, int n = 15)
        {
            var result = await _wordVectors.Similar(a);
            if (result == null)
            {
                await TypingReplyAsync($"I don't know the word `{a}`");
                return;
            }

            await DisplayItemList(
                result.Take(n).ToArray(),
                () => "I can't find any similar words",
                items => $"The {items.Count} most similar words are:",
                (t, i) => $"`{t.Word}` ({t.Similarity})"
            );
        }

        [Command("teach")]
        public async Task TeachWord(string word)
        {
            word = word.ToLowerInvariant();

            //Check if we already know this word, in which case we can just early exit
            var vector = await _wordVectors.Vector(word);
            if (vector != null)
            {
                await TypingReplyAsync("I already know what {word} means!");
                return;
            }

            //Prompt user for examples
            await TypingReplyAsync($"I don't know what `{word}` means, can you use it in some example sentences?");

            //Watch all messages in the channel for some time. Every message which contains the word will be taken as an example
            var timer = new Stopwatch();
            while (timer.Elapsed < TimeSpan.FromMinutes(1))
            {
                var message = await NextMessageAsync(false, true, TimeSpan.FromMinutes(1));
                if (message == null)
                    continue;

                var content = message.Content.ToLower();
                if (!content.Contains(word))
                    continue;

                await _training.Teach(word, content);
                timer.Restart();
                if (message is IUserMessage um)
                    await um.AddReactionAsync(new Emoji(EmojiLookup.OpenBook));
            }
        }
    }
}
