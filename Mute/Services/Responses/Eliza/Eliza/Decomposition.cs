// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System.Collections.Generic;

namespace Mute.Services.Responses.Eliza.Eliza
{
	public class Decomposition
	{
	    public string Pattern { get; }

	    public bool Memorise { get; }

	    public bool Randomise { get; }

	    public IReadOnlyList<string> Reassemblies { get; }

		internal Decomposition(string pattern, bool memorise, bool randomise, IReadOnlyList<string> reassemblies)
		{
			Pattern = pattern;
			Memorise = memorise;
		    Randomise = randomise;
			Reassemblies = reassemblies;
		}
	}
}
