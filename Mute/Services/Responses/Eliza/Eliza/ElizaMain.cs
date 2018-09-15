// class translated from Java
// Credit goes to Charles Hayden http://www.chayden.net/eliza/Eliza.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services.Responses.Eliza.Eliza
{
	public sealed class ElizaMain
	{
	    private readonly Script _script;
	    private readonly Key _xnone;
	    private readonly Random _random;
	    private readonly Stack<string> _mem = new Stack<string>();
	    private readonly Dictionary<Decomposition, int> _decompositionCount = new Dictionary<Decomposition, int>();

	    public bool Finished { get; private set; }

	    public ElizaMain(Script script, int seed)
		{
		    _random = new Random(seed);
		    _script = script;

		    _script.Keys.TryGetValue("xnone", out _xnone);
		}

	    [NotNull] public string ProcessInput(string input)
	    {
	        input = CleanInput(input);

            return ProcessSentences(input)      //Try to create a reply to one of the sentences of the input
                ?? _mem.PopOrDefault()          //Fall back to a reply we saved earlier
                ?? TryKey(_xnone, input)     //Make a default reply
                ?? "I am at a loss for words";  //Return default default
	    }

	    [CanBeNull] private string ProcessSentences([NotNull] string input)
	    {
	        return (from sentence in input.Split('.', StringSplitOptions.RemoveEmptyEntries)
                    let transformed = Transform(_script.Pre, sentence).Trim()
	                let r = Sentence(sentence)
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
		               let key = _script.Keys.GetValueOrDefault(word)
		               where key != null
		               orderby key.Rank descending
		               select key;

		    //Get a reply for each key
		    var replies = from key in keys
		                  let reply = TryKey(key, sentence)
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
		[CanBeNull] private string TryKey([NotNull] Key key, string sentence)
		{
		    // Decomposition match, If decomp has no synonyms, do a regular match.
		    bool MatchDecomp(string str, string pat, string[] lines)
		    {
		        if (!Patterns.Match(pat, "*@* *", lines))
		        {
		            //  no synonyms in decomp pattern
		            return Patterns.Match(str, pat, lines);
		        }
		        //  Decomp pattern has synonym -- isolate the synonym
		        var first = lines[0];
		        var synWord = lines[1];
		        var theRest = " " + lines[2];
		        //  Look up the synonym
		        var syn = _script.Syns.FirstOrDefault(w => w.Contains(synWord));
		        if (syn == null)
		        {
		            return false;
		        }
		        //  Try each synonym individually
		        for (var i = 0; i < syn.Count; i++)
		        {
		            //  Make a modified pattern
		            pat = first + syn[i] + theRest;
		            if (Patterns.Match(str, pat, lines))
		            {
		                var n = first.Count(a => a == '*');
		                //  Make room for the synonym in the match list.
		                for (var j = lines.Length - 2; j >= n; j--)
		                {
		                    lines[j + 1] = lines[j];
		                }
		                //  The synonym goes in the match list.
		                lines[n] = syn[i];
		                return true;
		            }
		        }
		        return false;
		    }

			var reply = new string[10];
			for (var i = 0; i < key.Decompositions.Count; i++)
			{
				var d = key.Decompositions[i];
				var pat = d.Pattern;
				if (MatchDecomp(sentence, pat, reply))
				{
				    var rep = Assemble(d, reply, out var gotoKey);
					if (rep != null)
						return rep;

				    if (gotoKey?.Keyword != null)
				        return TryKey(gotoKey, sentence);
				}
			}
			return null;
		}

	    /// <summary>Assembly a reply from a decomp rule and the input.</summary>
		/// <remarks>
		/// Assembly a reply from a decomp rule and the input. If the reassembly rule
		/// is goto, return null and give the gotoKey to use. Otherwise return the
		/// response.
		/// </remarks>
		[CanBeNull] private string Assemble([NotNull] Decomposition d, string[] reply, [CanBeNull] out Key gotoKey)
	    {
            //Cycle through the rules in order
	        if (!_decompositionCount.ContainsKey(d))
	            _decompositionCount[d] = 0;
            var rule = d.Randomise
                     ? d.Reassemblies.Random(_random)
                     : d.Reassemblies[_decompositionCount[d] % d.Reassemblies.Count];
	        _decompositionCount[d]++;

			var lines = new string[3];

            //Early exit if this is a goto rule
	        if (Patterns.Match(rule, "goto *", lines))
	        {
	            if (_script.Keys.TryGetValue(lines[0], out gotoKey))
	                if (gotoKey?.Keyword != null)
	                    return null;
	            return null;
	        }
	        else
	            gotoKey = null;

            //Substitute synonyms
	        var words = rule.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	        for (var i = 0; i < words.Length; i++)
	            if (words[i].Contains('@'))
	                words[i] = _script.Syns.FirstOrDefault(w => w.Contains(words[i].TrimStart('@'))).Random(_random);
	        rule = string.Join(" ", words);

	        var work = "";
			while (Patterns.Match(rule, "* (#)*", lines))
			{
				// reassembly rule with number substitution
				rule = lines[2];

                //Parse the number
                if (!int.TryParse(lines[1], out var n))
                {
                    Console.WriteLine("Number is wrong in reassembly rule " + lines[1]);
                    return null;
                }

                //move back from 1 based indexing to zero indexing
			    n--;

                //Check if idnex is out of range
			    if (n < 0 || n >= reply.Length)
					return null;

				reply[n] = Transform(_script.Post, reply[n]);
				work += lines[0] + " " + reply[n];
			}

	        work += rule;

	        if (d.Memorise)
			{
				_mem.Push(work);
				return null;
			}
			return work;
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
	        return string.Join(" ",
	            from word in s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
	            let tx = transformations.GetValueOrDefault(word)?.Destination ?? word
	            select tx
	        );
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
