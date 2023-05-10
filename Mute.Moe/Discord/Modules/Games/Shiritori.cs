using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules.Games;

[UsedImplicitly]
[HelpGroup("games")]
[Group("shiritori")]
public class Shiritori
    : BaseModule
{
    private readonly HashSet<string> _words;
    private readonly Random _random;

    public Shiritori(Configuration config, Random random)
    {
        if (config.Dictionary == null)
            throw new ArgumentNullException(nameof(config.Dictionary));
        if (config.Dictionary.WordListPath == null)
            throw new ArgumentNullException(nameof(config.Dictionary.WordListPath));

        _words = new(File.ReadAllLines(config.Dictionary.WordListPath));
        _random = random;
    }

    [Command("help"), Summary("I will briefly explain the rules of the game")]
    public async Task Help()
    {
        await TypingReplyAsync("Shiritori is a word game. I will say a word such as `staR` and then you follow with a word which starts with the ending letter, for example `Root`");
        await TypingReplyAsync("Words must be 4 or more letters. The loser is the first to repeat a word or fail to follow a word.");
        await TypingReplyAsync("Type `!shiritori` to start a game, you can optionally specify a difficulty mode (Easy|Normal|Hard|Impossible)");
    }

    [Command, Summary("Challenge me to a game of shiritori")]
    public async Task StartGame(Mode mode = Mode.Normal)
    {
        await StartGame(mode, auto:false);
    }

    [Command, RequireOwner, Summary("Challenge me to a game of shiritori (or force me to play off against myself)")]
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public async Task StartGame(Mode mode, Mode autoMode = Mode.Hard, bool auto = true)
    {
        var values = Get(mode);

        string? prev = null;
        var played = new HashSet<string>();

        while (played.Count < values.TurnLimit)
        {
            {
                //Check if we failed to find a valid response
                var myWord = Pick(prev, played, mode, values);
                if (myWord == null)
                {
                    await TypingReplyAsync("I can't think of a good followup for that. You win!");
                    return;
                }

                //Play our turn
                await TypingReplyAsync($"{played.Count + 1}. {myWord}");
                prev = myWord;
                played.Add(myWord);
            }

            string? theirWord;
            if (auto)
            {
                //Generate a response
                theirWord = Pick(prev, played, autoMode, Get(autoMode));
                if (theirWord == null || string.IsNullOrEmpty(theirWord))
                {
                    await TypingReplyAsync("I can't think of a good followup for that. You win!");
                    return;
                }

                await TypingReplyAsync(theirWord);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            else
            {
                //Get response, break out if they lose (too slow)
                var response = await NextMessageAsync(true, true, values.TimeLimit);
                if (response == null)
                {
                    await TypingReplyAsync("Too slow! I win :D");
                    return;
                }

                theirWord = response.Content.ToLowerInvariant();
            }

            //Check for repeats
            if (played.Contains(theirWord))
            {
                await TypingReplyAsync("That word was already played! I win :D");
                return;
            }

            //Check length rule
            if (theirWord.Length < 4)
            {
                await TypingReplyAsync("Too short! I win :D");
                return;
            }

            //Check letters
            if (theirWord.First() != prev.Last())
            {
                await TypingReplyAsync("Wrong letter! I win :D");
                return;
            }

            //Check that this word is in the dictionary
            if (!_words.Contains(theirWord))
            {
                await TypingReplyAsync("That's not a real word! I win :D");
                return;
            }

            //They pass, add word to game state
            prev = theirWord;
            played.Add(theirWord);
        }

        await TypingReplyAsync($"Wow, {played.Count} turns! I surrender, you win.");
    }

    private string? Pick(string? previous, ICollection<string> played, Mode mode, DifficultyValues values)
    {
        //Pick a random starting word
        if (previous == null)
            return _words.Where(a => a.Length >= 4).Random(_random);

        //Even in hard mode occasionally default back to normal mode, just to mix it up a bit
        if (_random.NextDouble() < values.RevertToNormalChance)
            mode = Mode.Normal;

        //Sometimes just don't return a word
        if (_random.NextDouble() < values.PerTurnNoWordChance)
            return null;

        //Get set of all valid words
        var targetChar = previous.Last();
        var nextWords = _words
            .Where(a => a.Length >= values.MinWordLength && a.Length <= values.MaxWordLength && _random.NextDouble() > values.PerWordSkipChance)
            .Where(a => a[0] == targetChar)
            .Where(a => !played.Contains(a));

        switch (mode)
        {
            //Pick a random valid word.
            case Mode.Easy:
            case Mode.Normal: {
                return SelectWord(nextWords.Take(350));
            }

            //Specifically try to pick a word which starts with the same character as a previous word
            case Mode.Hard:
            case Mode.Impossible: {

                //Get groups of all previous words, grouped by start character, ordered descrnding by length
                var grouped = played.GroupBy(a => a.First()).OrderByDescending(a => a.Count()).ToArray();

                //Pick the character group which has been used the most
                var chars = grouped[0];

                //Pick a word ending with that character
                var options = nextWords
                    .Where(a => a.Last() == chars.Key)
                    .Take(350);

                return SelectWord(options);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    private static string SelectWord(IEnumerable<string> options)
    {
        return options
              .Shuffle()
              .First();
    }

    private static DifficultyValues Get(Mode mode)
    {
        return mode switch {
            Mode.Easy => new DifficultyValues {
                TurnLimit = 10,
                PerTurnNoWordChance = 0.1f,
                TimeLimit = TimeSpan.FromMinutes(1),
                RevertToNormalChance = 0.1,
                PerWordSkipChance = 0.1f,
                MinWordLength = 4,
                MaxWordLength = 8,
            },
            Mode.Normal => new DifficultyValues {
                TurnLimit = 30,
                PerTurnNoWordChance = 0.01f,
                TimeLimit = TimeSpan.FromSeconds(45),
                RevertToNormalChance = 0,
                PerWordSkipChance = 0.05f,
                MinWordLength = 4,
                MaxWordLength = 9,
            },
            Mode.Hard => new DifficultyValues {
                TurnLimit = 50,
                PerTurnNoWordChance = 0.0f,
                TimeLimit = TimeSpan.FromSeconds(30),
                RevertToNormalChance = 0.75,
                PerWordSkipChance = 0.05f,
                MinWordLength = 4,
                MaxWordLength = 12,
            },
            Mode.Impossible => new DifficultyValues {
                TurnLimit = int.MaxValue,
                PerTurnNoWordChance = 0.0f,
                TimeLimit = TimeSpan.FromSeconds(20),
                RevertToNormalChance = 0.15,
                PerWordSkipChance = 0.01f,
                MinWordLength = 4,
                MaxWordLength = int.MaxValue,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
    }

    private struct DifficultyValues
    {
        public int TurnLimit;
        public float PerTurnNoWordChance;
        public TimeSpan TimeLimit;
        public double RevertToNormalChance;
        public double PerWordSkipChance;
        public int MinWordLength;
        public int MaxWordLength;
    }

    public enum Mode
    {
        Easy,
        Normal,
        Hard,
        Impossible,
    }
}