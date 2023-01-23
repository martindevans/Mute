﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Words;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("word")]
public class Words
    : BaseModule
{
    private readonly IWords _wordVectors;
    private readonly IWordTraining _training;

    public Words(IWords wordVectors, IWordTraining training)
    {
        _wordVectors = wordVectors;
        _training = training;
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

        if (!result.HasValue)
        {
            var av = await _wordVectors.Vector(a);
            if (av == null)
                await TypingReplyAsync($"I don't know the word `{a}`");
            var bv = await _wordVectors.Vector(b);
            if (bv == null)
                await TypingReplyAsync($"I don't know the word `{b}`");
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
            (t, _) => $"`{t.Word}` ({t.Similarity})"
        );
    }

    [Command("teach"), Hidden]
    public async Task TeachWord(string word)
    {
        word = word.ToLowerInvariant();

        //Check if we already know this word, in which case we can just early exit
        var vector = await _wordVectors.Vector(word);
        if (vector != null)
        {
            await TypingReplyAsync($"I already know what {word} means!");
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

            await _training.Train(word, content);
            timer.Restart();
            if (message is IUserMessage um)
            {
                await um.AddReactionAsync(new Emoji(EmojiLookup.OpenBook));
                await um.AddReactionAsync(new Emoji(EmojiLookup.Tick));
            }
        }
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