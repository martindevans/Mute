using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Mute.Services.Responses.Eliza.Engine
{
	public class Decomposition
	{
	    public string Pattern { get; }
	    public bool Memorise { get; }
	    public bool Randomise { get; }
	    public IReadOnlyList<IReassembly> Reassemblies { get; }

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

	    public Decomposition(string pattern, bool memorise, bool randomise, [NotNull] params Func<IReadOnlyList<string>, string>[] reassemblies)
	        : this(pattern, memorise, randomise, reassemblies.Select(f => new FuncReassembly(f)).ToArray<IReassembly>())
	    {
	    }
	}

    public interface IReassembly
    {
        string Rule(IReadOnlyList<string> decomposition);
    }

    public class ConstantReassembly
        : IReassembly
    {
        private readonly string _value;

        public ConstantReassembly(string value)
        {
            _value = value;
        }

        public string Rule(IReadOnlyList<string> decomposition) => _value;
    }

    public class FuncReassembly
        : IReassembly
    {
        private Func<IReadOnlyList<string>, string> _func;

        public FuncReassembly(Func<IReadOnlyList<string>, string> func)
        {
            _func = func;
        }

        public string Rule(IReadOnlyList<string> decomposition) => _func(decomposition);
    }
}
