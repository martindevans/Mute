using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Words;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("word")]
public class Words
    : BaseModule
{
    private readonly IWords _wordVectors;

    public Words(IWords wordVectors)
    {
        _wordVectors = wordVectors;
    }

    [Command("vector"), Summary("I will get the raw vector for a word"), Hidden]
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

        switch (result)
        {
            case null:
            {
                var av = await _wordVectors.Vector(a);
                if (av == null)
                    await TypingReplyAsync($"I don't know the word `{a}`");
                var bv = await _wordVectors.Vector(b);
                if (bv == null)
                    await TypingReplyAsync($"I don't know the word `{b}`");
                break;
            }
            case < 0.2:
                await TypingReplyAsync($"`{a}` and `{b}` are very different ({result:0.0##})");
                break;
            case < 0.5:
                await TypingReplyAsync($"`{a}` and `{b}` are not very similar ({result:0.0##})");
                break;
            case < 0.65:
                await TypingReplyAsync($"`{a}` and `{b}` are a bit similar ({result:0.0##})");
                break;
            case < 0.85:
                await TypingReplyAsync($"`{a}` and `{b}` are quite similar ({result:0.0##})");
                break;
            default:
                await TypingReplyAsync($"`{a}` and `{b}` are analagous ({result:0.0##})");
                break;
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
            (t, _) => $"`{t.Word}` ({t.Similarity})"
        );
    }

    [Command("lerp")]
    public async Task LerpWords(string start, string end, int sampling = 32)
    {
        var a = await _wordVectors.Vector(start);
        if (a == null)
        {
            await TypingReplyAsync($"I don't the word '{start}'");
            return;
        }

        var b = await _wordVectors.Vector(end);
        if (b == null)
        {
            await TypingReplyAsync($"I don't the word '{end}'");
            return;
        }

        var results = new HashSet<string> { start };
        var current = start;
        for (var i = 0; i < 128; i++)
        {
            // Find words similar to the current word
            var similar = await _wordVectors.Similar(current);
            if (similar == null)
            {
                await TypingReplyAsync($"I can't find any similar words to `{current}` :(");
                return;
            }

            // Find the one which is most similar to the _end_ word
            var bestScore = double.NegativeInfinity;
            string? closest = null;
            foreach (var candidate in similar.Take(sampling))
            {
                var candidateWord = candidate.Word;
                if (results.Contains(candidateWord))
                    continue;
                
                var similarity = await _wordVectors.Similarity(candidateWord, end);
                if (similarity > bestScore)
                {
                    bestScore = similarity.Value;
                    closest = candidateWord;
                }
            }

            if (closest == null)
            {
                await TypingReplyAsync("I can't find any more similar words :(");
                return;
            }

            current =  closest;
            results.Add(closest);
            await TypingReplyAsync($"{i + 1}. {closest} ({bestScore})");
            await Task.Delay(25);

            if (closest == end)
                break;
        }
    }
}