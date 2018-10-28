using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Engine
{
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

	    public Decomposition(string pattern, bool memorise, bool randomise, [NotNull] params string[] reassemblies)
            : this(pattern, memorise, randomise, reassemblies.Select(r => new ConstantReassembly(r)).ToArray<IReassembly>())
	    {
	    }


	    public Decomposition(string pattern, bool memorise, bool randomise, [NotNull] params Func<ICommandContext, IReadOnlyList<string>, Task<string>>[] reassemblies)
	        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly(f)).ToArray<IReassembly>())
	    {
	    }

	    public Decomposition(string pattern, bool memorise, bool randomise, [NotNull] params Func<ICommandContext, IReadOnlyList<string>, string>[] reassemblies)
	        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly((c, s) => Task.FromResult(f(c, s)))).ToArray<IReassembly>())
	    {
	    }

	    public Decomposition(string pattern, [NotNull] params Func<ICommandContext, IReadOnlyList<string>, Task<string>>[] reassemblies)
	        : this(pattern, reassemblies.Select(f => new FuncReassembly(f)).ToArray<IReassembly>())
	    {
	    }


	    public Decomposition(string pattern, bool memorise, bool randomise, [NotNull] params Func<IReadOnlyList<string>, Task<string>>[] reassemblies)
	        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly((_, i) => f(i))).ToArray<IReassembly>())
	    {
	    }

	    public Decomposition(string pattern, [NotNull] params Func<IReadOnlyList<string>, string>[] reassemblies)
	        : this(pattern, reassemblies.Select(f => new FuncReassembly((_, s) => Task.FromResult(f(s)))).ToArray<IReassembly>())
	    {
	    }

	    public Decomposition(string pattern, [NotNull] params Func<IReadOnlyList<string>, Task<string>>[] reassemblies)
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

	    public Decomposition(string pattern, [NotNull] params string[] reassemblies)
	        : this(pattern, reassemblies.Select(r => new ConstantReassembly(r)).ToArray<IReassembly>())
	    {
	    }
        #endregion
	}
}
