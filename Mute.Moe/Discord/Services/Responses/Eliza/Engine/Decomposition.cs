using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace Mute.Moe.Discord.Services.Responses.Eliza.Engine;

/// <summary>
/// A single pattern to match against a string, and a set of reassemblies to assemble a response when the pattern matches
/// </summary>
public class Decomposition
{
    /// <summary>
    /// Pattern to match against an input string
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Whether replies generated with this decompisition should be "memorised" for later use or used immediately as a reply
    /// </summary>
    public bool Memorise { get; }

    /// <summary>
    /// Whether a random reassembly rule should be selected (or else they will be used in order)
    /// </summary>
    public bool Randomise { get; }

    /// <summary>
    /// All the reassembly rules
    /// </summary>
    public IReadOnlyList<IReassembly> Reassemblies { get; }

    private string? _regexCache;

    #region constructors
    public Decomposition(string pattern, bool memorise, bool randomise, IReadOnlyList<IReassembly> reassemblies)
    {
        Pattern = pattern;
        Memorise = memorise;
        Randomise = randomise;
        Reassemblies = reassemblies;
    }

    private Decomposition(string pattern, bool memorise, bool randomise, params IReassembly[] reassemblies)
        : this(pattern, memorise, randomise, (IReadOnlyList<IReassembly>)reassemblies)
    {
    }

    public Decomposition(string pattern, bool memorise, bool randomise,  params string[] reassemblies)
        : this(pattern, memorise, randomise, reassemblies.Select(r => new ConstantReassembly(r)).ToArray<IReassembly>())
    {
    }


    public Decomposition(string pattern, bool memorise, bool randomise,  params Func<ICommandContext, IReadOnlyList<string>, Task<string?>>[] reassemblies)
        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly(f)).ToArray<IReassembly>())
    {
    }

    public Decomposition(string pattern, bool memorise, bool randomise,  params Func<ICommandContext, IReadOnlyList<string?>, string>[] reassemblies)
        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly((c, s) => Task.FromResult<string?>(f(c, s)))).ToArray<IReassembly>())
    {
    }

    public Decomposition(string pattern,  params Func<ICommandContext, IReadOnlyList<string>, Task<string?>>[] reassemblies)
        : this(pattern, reassemblies.Select(f => new FuncReassembly(f)).ToArray<IReassembly>())
    {
    }


    public Decomposition(string pattern, bool memorise, bool randomise,  params Func<IReadOnlyList<string>, Task<string?>>[] reassemblies)
        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly((_, i) => f(i))).ToArray<IReassembly>())
    {
    }

    public Decomposition(string pattern,  params Func<IReadOnlyList<string>, string?>[] reassemblies)
        : this(pattern, reassemblies.Select(f => new FuncReassembly((_, s) => Task.FromResult(f(s)))).ToArray<IReassembly>())
    {
    }

    public Decomposition(string pattern,  params Func<IReadOnlyList<string>, Task<string?>>[] reassemblies)
        : this(pattern, reassemblies.Select(f => new FuncReassembly((_, i) => f(i))).ToArray<IReassembly>())
    {
    }


    private Decomposition(string pattern, IReadOnlyList<IReassembly> reassemblies)
        : this(pattern, false, false, reassemblies)
    {
    }

    private Decomposition(string pattern, params IReassembly[] reassemblies)
        : this(pattern, (IReadOnlyList<IReassembly>)reassemblies)
    {
    }

    public Decomposition(string pattern,  params string[] reassemblies)
        : this(pattern, reassemblies.Select(r => new ConstantReassembly(r)).ToArray<IReassembly>())
    {
    }
    #endregion

    #region decompose
    /// <summary>
    /// Try to match an input string. If successful, return the parts of the input string which can be used for recomposition.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="synonyms"></param>
    /// <returns></returns>
    public IReadOnlyList<string>? Match(string str, IReadOnlyList<IReadOnlyCollection<string>> synonyms)
    {
        _regexCache ??= BuildRegex(Pattern, synonyms);

        var match = Regex.Match(str, _regexCache, RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        return match.Groups.Skip<Group>(1).Select(m => m.Value).ToArray();
    }

    private static string BuildRegex(string pattern, IReadOnlyList<IReadOnlyCollection<string>> synonyms)
    {
        //Transform pattern into a regex
        var regexPattern = new StringBuilder(pattern.Length);
        regexPattern.Append('^');

        for (var i = 0; i < pattern.Length; i++)
        {
            var character = pattern[i];
            switch (character)
            {
                case '*':
                    regexPattern.Append("(.*?)");
                    break;

                case '#':
                    regexPattern.Append("(\\d+)");
                    break;

                case '@':
                    i += BuildSynonym(i, pattern, regexPattern, synonyms);
                    break;

                default:
                    regexPattern.Append(Regex.Escape(character.ToString()));
                    break;
            }
        }

        regexPattern.Append('$');
        return regexPattern.ToString();
    }

    private static int BuildSynonym(int index, string pat, StringBuilder regex, IEnumerable<IReadOnlyCollection<string>> synonyms)
    {
        int FindEndOfWord()
        {
            for (var i = index + 1; i < pat.Length; i++)
            {
                var c = pat[i];
                if (!char.IsLetter(c))
                    return i;
            }

            return pat.Length;
        }

        var end = FindEndOfWord();

        var word = pat.Substring(index + 1, end - index - 1);
        var syns = synonyms.FirstOrDefault(a => a.Contains(word)) ?? new [] { word };

        regex.Append("(");
        regex.Append(string.Join('|', syns));
        regex.Append(")");

        return word.Length;
    }
    #endregion
}