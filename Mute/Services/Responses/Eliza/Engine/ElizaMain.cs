using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services.Responses.Eliza.Scripts;

namespace Mute.Services.Responses.Eliza.Engine
{
	public sealed class ElizaMain
	{
	    private readonly Script _script;
	    private readonly IReadOnlyList<Key> _xnone;
	    private readonly Random _random;
	    private readonly Stack<string> _mem = new Stack<string>();
	    private readonly Dictionary<Decomposition, int> _decompositionCount = new Dictionary<Decomposition, int>();

	    public bool Finished { get; private set; }

	    public ElizaMain(Script script, int seed)
		{
		    _random = new Random(seed);
		    _script = script;

		    _xnone = _script.GetKeys("xnone").ToArray();
		}

	    [NotNull] public string ProcessInput(string input)
	    {
	        input = CleanInput(input);

            return ProcessSentences(input)      //Try to create a reply to one of the sentences of the input
                ?? _mem.PopOrDefault()          //Fall back to a reply we saved earlier
                ?? TryKey(_xnone.Random(_random), input)     //Make a default reply
                ?? "I am at a loss for words";  //Return default default
	    }

	    [CanBeNull] private string ProcessSentences([NotNull] string input)
	    {
	        return (from sentence in input.Split('.', StringSplitOptions.RemoveEmptyEntries)
                    let transformed = Transform(_script.Pre, sentence).Trim()
	                let r = Sentence(transformed)
	                where r != null
	                select r).FirstOrDefault();
	    }

		[CanBeNull] private string Sentence([NotNull] string sentence)
		{
            //Split sentence into words
		    var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		    //Check if there are any exit words, if so immediately terminate
		    if (words.Any(_script.Quit.Contains))
		    {
		        Finished = true;
		        return _script.Final.Random(_random);
		    }

		    //Get key for each word in the sentence, ordered by rank
		    var keys = from word in words
                       from key in _script.GetKeys(word)
		               where key != null
		               orderby key.Rank descending
		               select key;

		    //Get a reply for each key
		    var replies = from key in keys
		                  let reply = TryKey(key, sentence.PadLeft(1).PadRight(1))
		                  where reply != null
		                  select reply;

		    //take the first non-null reply
		    return replies.FirstOrDefault();
		}

		/// <summary>Decompose a string according to the given key.</summary>
		/// <remarks>
		/// Decompose a string according to the given key. Try each decomposition
		/// rule in order. If it matches, assemble a reply and return it. If assembly
		/// fails, try another decomposition rule. If assembly is a goto rule, return
		/// null and give the key. If assembly succeeds, return the reply;
		/// </remarks>
		[CanBeNull] private string TryKey([CanBeNull] Key key, string sentence)
		{
		    if (key == null)
		        return null;

            //Select reassembly rules which match the input
		    var decompositions = from decomp in key.Decompositions
		                         let decomposed = Patterns.Match(sentence, decomp.Pattern, _script.Syns)
		                         where decomposed != null
                                 let rule = ChooseReassembly(decomp).Rule(decomposed)
                                 where !string.IsNullOrWhiteSpace(rule)
		                         select (decomp, rule, decomposed);

            foreach (var (decomposition, rule, decomposed) in decompositions)
            {
                //If it's a goto rule follow it
                if (rule.StartsWith("goto "))
                {
                    var gotos = _script.GetKeys(rule.Substring(5));
                    return (from g in gotos
                            orderby g.Rank descending
                            let r = TryKey(g, sentence)
                            where r != null
                            select r).FirstOrDefault();
                }

                //Try to assemble a reply using this reassembly rule
                var rep = Assemble(rule, decomposed);
                if (rep != null)
                {
                    if (decomposition.Memorise)
                        _mem.Push(rep);
                    else
                        return rep;
                }
            }

			return null;
		}

	    private IReassembly ChooseReassembly([NotNull] Decomposition d)
	    {
	        //Initialize index for this rule if it's not already set
	        if (!_decompositionCount.ContainsKey(d))
	            _decompositionCount[d] = 0;

	        //Choose the rule (either cycle in order, or choose a random one)
	        var rule = d.Randomise
	            ? d.Reassemblies.Random(_random)
	            : d.Reassemblies[_decompositionCount[d] % d.Reassemblies.Count];

	        //Inc index so that next time we select a different rule
	        _decompositionCount[d]++;

	        return rule;
	    }

	    /// <summary>Assembly a reply from a decomp rule and the input.</summary>
		/// <remarks>
		/// Assembly a reply from a decomp rule and the input. If the reassembly rule
		/// is goto, return null and give the gotoKey to use. Otherwise return the
		/// response.
		/// </remarks>
		[CanBeNull] private string Assemble(string reassembly, IReadOnlyList<string> decomposed)
	    {
	        var response = new StringBuilder(reassembly);

            //Find substitutions `(n)` and replace them with the correct parts from the decomposed string
	        const string r = "\\((?<num>\\d+)\\)";
	        Match m;
	        do
	        {
	            m = Regex.Match(response.ToString(), r);
	            if (m.Success)
	            {
	                if (!int.TryParse(m.Groups["num"].Value, out var n) || n > decomposed.Count)
	                    return null;

	                var replacement = decomposed[n - 1];
	                if (replacement == null)
	                    return null;

	                var transformed = Transform(_script.Post, replacement);

	                response.Replace(m.Value, transformed, m.Index, m.Length);
	            }
	        } while (m.Success);

	        return response.ToString();
	    }

        #region static helpers
	    private static string CleanInput([NotNull] string input)
	    {
	        void Compress(StringBuilder str)
	        {
	            if (str.Length == 0)
	                return;

	            //Keep replacing runs of 2 spaces with a single space until we don't do any more
	            int l;
	            do
	            {
	                l = str.Length;
	                str.Replace("  ", " ");
	            } while (l != str.Length);
	        }

	        var builder = new StringBuilder(input.ToLowerInvariant());

	        foreach (var character in "@#$%^&*()_-+=~`{[}]|:;<>\\\"")
	            builder.Replace(character, ' ');

	        foreach (var c in ",?!-")
	            builder.Replace(c, '.');

	        Compress(builder);

	        return builder.ToString();
	    }

	    [NotNull] private static string Transform([NotNull] IReadOnlyDictionary<string, Transform> transformations, [NotNull] string s)
	    {
	        var txs = from word in s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
	                  let tx = transformations.GetValueOrDefault(word)
	                  select (word, tx);

	        var result = from tx in txs
	                     select tx.Item2?.Destination ?? tx.Item1;

	        return string.Join(" ", result);
	    }
        #endregion

	    public override string ToString()
	    {
	        var b = new StringBuilder();

	        b.AppendLine($"Script:{_script}");
	        b.AppendLine($"Memory ({_mem.Count} items):");
	        foreach (var item in _mem)
	            b.AppendLine($" - {item}");

	        return b.ToString();
	    }
	}
}
