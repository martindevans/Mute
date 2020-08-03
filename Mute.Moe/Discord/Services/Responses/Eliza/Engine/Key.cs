using System.Collections.Generic;

namespace Mute.Moe.Discord.Services.Responses.Eliza.Engine
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

		internal Key(string keyword, int rank, params Decomposition[] decompositions)
		{
			Keyword = keyword;
			Rank = rank;
		    Decompositions = decompositions;
		}

        public Key(string keyword, params Decomposition[] decompositions)
		    : this(keyword, 10, decompositions)
        {
            
        }
	}
}
