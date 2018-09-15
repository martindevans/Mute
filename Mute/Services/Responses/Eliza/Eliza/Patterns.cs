// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Eliza
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
	    /// </remarks>
	    [ContractAnnotation("=> true, matches:notnull; => false, matches:null;")]
		public static bool Match([NotNull] string str, [NotNull] string pat, string[] matches)
	    {
            //Transform pattern into a regex
	        var regexPattern = new StringBuilder(pat.Length);
	        regexPattern.Append("^");
	        foreach (var character in pat)
	        {
	            switch (character)
	            {
                    case '*':
                        regexPattern.Append("(.*?)");
                        break;

                    case '#':
                        regexPattern.Append("(\\d+)");
                        break;

                    default:
                        regexPattern.Append(Regex.Escape(character.ToString()));
                        break;
	            }
	        }
	        regexPattern.Append("$");

	        var match = Regex.Match(str, regexPattern.ToString(), RegexOptions.IgnoreCase);
	        if (!match.Success)
	        {
	            matches = null;
	            return false;
	        }

	        match.Groups.Skip(1).Select(m => m.Value).ToArray().CopyTo(matches, 0);

	        return true;
		}
	}
}
