using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Services.Responses
{
    public class UrbitSarcasm
        : IResponse
    {
        public double BaseChance => 0.15;
        public double MentionedChance => 1;

        private readonly Random _random;

        private readonly HashSet<string> _triggerWords = new HashSet<string> {
            "urbit", "hoon", "arvo", "nock"
        };

        public UrbitSarcasm(Random random)
        {
            _random = random;
        }

        public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            var msg = rgx.Replace(context.Message.Content, "");

            var words = msg.ToLowerInvariant().Split(' ');
            if (_triggerWords.Overlaps(words))
                return new UrbitConversation(Sarcasm());
            else
                return null;
        }

        private class UrbitConversation
            : TerminalConversation
        {
            public UrbitConversation(string response)
                : base(response)
            {
            }
        }

        #region response generator
         private static string Plural( string noun)
        {
            var suffix = (noun.EndsWith('s') || noun.EndsWith("sh")) ? "es" : "s";
            return noun + suffix;
        }

         private static string Article( string word)
        {
            return Regex.IsMatch(word, "^[aeiou]") ? "an" : "a";
        }

         private string Sarcasm()
        {
            var intro = _intros.Random(_random);
            var thirdPerson = intro.Contains("Hoon");
            var verb = thirdPerson ? Plural(_verbs.Random(_random)) : _verbs.Random(_random);
            var noun1 = _nouns.Random(_random);
            var noun2 = _nouns.Random(_random);
            var noun3 = _nouns.Random(_random);
            var adjective1 = _adjectives.Random(_random);
            var adjective2 = _adjectives.Random(_random);
            var produce = thirdPerson ? "produces" : "produce";
            var rune1 = _runes.Random(_random);
            var rune2 = _runes.Random(_random);

            var result = $"Urbit is easy! {intro} {verb} {Article(noun1)} "
                       + $"{noun1} with {Article(adjective1)} {adjective1} {noun2} "
                       + $"using the {rune1.Item1}{rune2.Item1} ({rune1.Item2}{rune2.Item2}) twig and {produce} "
                       + $"{adjective2} {Plural(noun3)}";

            if (_random.NextDouble() > 0.25)
                result += $" for {_ships.Random(_random)}";

            return result;
        }
        #endregion

        #region data
        private readonly IReadOnlyList<string> _intros = new[] {
            "Just write some Hoon that",
            "Just compose Nock formulas that",
            "Just use Arvo to"
        };

        private readonly IReadOnlyList<string> _ships = new[] {
            "~zod",
            "~fyr",
            "~ped",
            "~doznec",
            "~tasfyn-partyv",
            "~porned-fapped",
            "~torbyt^sogwyx"
        };

        private readonly IReadOnlyList<string> _verbs = new[] {
            "bunt",
            "unbunt",
            "slot",
            "slam",
            "turn",
            "reap",
            "poke",
            "peak",
            "abet",
            "stun",
            "cook",
            "crisp",
            "trip",
            "mint",
            "bone",
            "burn"
        };

        private readonly IReadOnlyList<string> _adjectives = new[] {
            "regular",
            "irregular",
            "moldy",
            "woody",
            "wet",
            "dry",
            "warm",
            "cold",
            "iron",
            "zinc",
            "lead",
            "gold",
            "bivariant",
            "invariant",
            "covariant",
            "contravariant",
            "lapidary",
            "ultralapidary",
            "running",
            "jogging"
        };

        private readonly IReadOnlyList<string> _nouns = new[] {
            "atom",
            "cell",
            "noun",
            "span",
            "mold",
            "icon",
            "gate",
            "payload",
            "sample",
            "core",
            "arm",
            "foot",
            "twig",
            "aura",
            "odor",
            "cord",
            "tape",
            "book",
            "page",
            "stem",
            "bulb",
            "moss",
            "seed",
            "rune",
            "sigil",
            "limb",
            "leg",
            "wing",
            "face",
            "taco",
            "tune",
            "alias",
            "bridge",
            "beak",
            "bull",
            "term",
            "toga",
            "vase",
            "mark",
            "generator",
            "galaxy",
            "star",
            "planet",
            "moon",
            "comet",
            "ship",
            "pier"
        };

        private readonly IReadOnlyList<ValueTuple<string, string>> _runes = new ValueTuple<string, string>[] {
            ("ace", " "),
            ("bar", "|"),
            ("bas", "\\"),
            ("buc", "$"),
            ("cab", "_"),
            ("cen", "%"),
            ("col", ":"),
            ("com", ","),
            ("doq", "'"),
            ("dot", "."),
            ("fas", "/"),
            ("gal", "<"),
            ("gap", "  "),
            ("gar", ">"),
            ("hax", "#"),
            ("hep", "-"),
            ("kel", "("),
            ("ker", ")"),
            ("ket", "^"),
            ("lus", "+"),
            ("pam", "&"),
            ("pat", "@"),
            ("pel", "("),
            ("per", ")"),
            ("sel", "("),
            ("sem", ";"),
            ("ser", ")"),
            ("soq", "\""),
            ("tar", "*"),
            ("tec", "`"),
            ("tis", "="),
            ("wut", "?"),
            ("zap", "!")
        };
        #endregion
    }
}
