// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Engine
{
    /// <summary>
    /// Decompose strings according to patterns
    /// </summary>
	public class Patterns
	{
	    /// <summary>
	    /// Match the string against a pattern and fills in
	    /// matches array with the pieces that matched * and #
	    /// </summary>
	    /// <remarks>
	    /// * matches any string (lazy)
	    /// # matches a number (eager)
	    /// @word matches word or any of it's synonyms
	    /// </remarks>
	    [CanBeNull] public static IReadOnlyList<string> Match([NotNull] string str, [NotNull] string pat, IReadOnlyList<IReadOnlyCollection<string>> synonyms)
	    {
            var regexPattern = BuildRegex(pat, synonyms);

	        var match = Regex.Match(str, regexPattern, RegexOptions.IgnoreCase);
	        if (!match.Success)
	            return null;

	        return match.Groups.Skip(1).Select(m => m.Value).ToArray();
		}

	    [NotNull] private static string BuildRegex([NotNull] string pattern, IReadOnlyList<IReadOnlyCollection<string>> synonyms)
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

	    private static int BuildSynonym(int index, [NotNull] string pat, [NotNull] StringBuilder regex, [NotNull] IEnumerable<IReadOnlyCollection<string>> synonyms)
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
	}
}
