// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Eliza
{
	public class Patterns
	{
	    /// <summary>Look for a match between the string and the pattern.</summary>
		/// <remarks>
		/// Look for a match between the string and the pattern.
		/// Return count of maching characters before * or #.
		/// Return -1 if strings do not match.
		/// </remarks>
	    private static int Amatch(int strOffset, [NotNull] string str, int patOffset, string pat)
		{
			var count = 0;

			var i = strOffset;
			var j = patOffset;
			while (i < str.Length && j < pat.Length)
			{
				var p = pat[j];

				// stop if pattern is * or #
				if (p == '*' || p == '#')
					return count;

				if (str[i] != p)
					return -1;

				i++;
				j++;
				count++;
			}

			return count;
		}

		/// <summary>
		/// Search in successive positions of the string,
		/// looking for a match to the pattern.
		/// </summary>
		/// <remarks>
		/// Search in successive positions of the string,
		/// looking for a match to the pattern.
		/// Return the string position in str of the match,
		/// or -1 for no match.
		/// </remarks>
		private static int FindPat(int strOffset, [NotNull] string str, int patOffset, string pat)
		{
			var count = 0;
			for (var i = strOffset; i < str.Length; i++)
			{
				if (Amatch(i, str, patOffset, pat) >= 0)
					return count;

				count++;
			}
			return -1;
		}

		/// <summary>Look for a number in the string.</summary>
		/// <remarks>
		/// Look for a number in the string.
		/// Return the number of digits at the beginning.
		/// </remarks>
		private static int FindNum([NotNull] string str)
		{
		    var match = Regex.Match(str, "[0-9]+");
		    if (match.Success)
		        return match.Length;

		    return -1;
		}
		internal static bool MatchB(string strIn, string patIn, string[] matches)
		{
			var str = strIn;
			var pat = patIn;
			var j = 0;
			//  move through matches
			while (pat.Length > 0 && str.Length >= 0 && j < matches.Length)
			{
				var p = pat[0];
				if (p == '*')
				{
					int n;
					if (pat.Length == 1)
					{
						//  * is the last thing in pat
						//  n is remaining string length
						n = str.Length;
					}
					else
					{
						//  * is not last in pat
						//  find using remaining pat
						n = FindPat(0, str, 1, pat);
					}
					if (n < 0)
					{
						return false;
					}

				    matches[j++] = str.Substring(0, n);
					str = str.Substring( n);
					pat = pat.Substring( 1);
				}
				else
				{
					if (p == '#')
					{
						var n = FindNum(str);
					    matches[j++] = str.Substring(0, n);
						str = str.Substring( n);
						pat = pat.Substring( 1);
					}
					else
					{
						//           } else if (p == ' ' && str.length() > 0 && str.charAt(0) != ' ') {
						//               pat = pat.substring(1);
						var n = Amatch(0, str, 0, pat);
						if (n <= 0)
						{
							return false;
						}
						str = str.Substring( n);
						pat = pat.Substring( n);
					}
				}
			}
			if (str.Length == 0 && pat.Length == 0)
			{
				return true;
			}
			return false;
		}

	    /// <summary>
	    /// Match the string against a pattern and fills in
	    /// matches array with the pieces that matched * and #
	    /// </summary>
		public static bool Match([NotNull] string str, [NotNull] string pat, string[] matches)
		{
		    var i = 0;
		    //  move through str
		    var j = 0;
		    //  move through matches
		    var pos = 0;
		    //  move through pat
		    while (pos < pat.Length && j < matches.Length)
		    {
		        var p = pat[pos];
		        if (p == '*')
		        {
		            int n;
		            if (pos + 1 == pat.Length)
		            {
		                //  * is the last thing in pat
		                //  n is remaining string length
		                n = str.Length - i;
		            }
		            else
		            {
		                //  * is not last in pat
		                //  find using remaining pat
		                n = FindPat(i, str, pos + 1, pat);
		            }
		            if (n < 0)
		            {
		                return false;
		            }

		            matches[j++] = str.Substring(i, n);
		            i += n;
		            pos++;
		        }
		        else
		        {
		            if (p == '#')
		            {
		                var n = FindNum(str.Substring(i));
		                matches[j++] = str.Substring(i, n);
		                i += n;
		                pos++;
		            }
		            else
		            {
		                var n = Amatch(i, str, pos, pat);
		                if (n <= 0)
		                {
		                    return false;
		                }
		                i += n;
		                pos += n;
		            }
		        }
		    }
		    if (i >= str.Length && pos >= pat.Length)
		    {
		        return true;
		    }
		    return false;
		}
	}
}