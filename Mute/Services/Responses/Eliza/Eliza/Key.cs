// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System.Collections.Generic;

namespace Mute.Services.Responses.Eliza.Eliza
{
	public sealed class Key
	{
	    public string Keyword { get; }

	    public int Rank { get; }

	    public IReadOnlyList<Decomposition> Decompositions { get; }

		internal Key(string keyword, int rank, IReadOnlyList<Decomposition> decompositions)
		{
			Keyword = keyword;
			Rank = rank;
		    Decompositions = decompositions;
		}
	}
}
