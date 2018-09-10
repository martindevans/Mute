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

		private readonly List<Key> _keyStack = new List<Key>();
		private readonly Stack<string> _mem = new Stack<string>();

	    private readonly Key _xnone;
	    private readonly Random _random;

	    public bool Finished { get; private set; }

	    public ElizaMain(Script script, int seed)
		{
		    _random = new Random(seed);
		    _script = script;

		    _script.Keys.TryGetValue("xnon", out _xnone);
		}

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

	        foreach (var c in ",?!")
	            builder.Replace(c, '.');

	        Compress(builder);

	        return builder.ToString();
	    }

	    [NotNull] public string ProcessInput(string input)
	    {
	        input = CleanInput(input);

			var lines = new string[2];

			// Break apart sentences, and do each separately.
			while (EString.Match(input, "*.*", lines))
			{
				var reply = Sentence(lines[0]);
				if (reply != null)
					return reply;

			    input = lines[1].TrimStart();
			}

			if (input.Length != 0)
			{
				var reply = Sentence(input);
				if (reply != null)
					return reply;
			}

			// Nothing matched, so try memory.
			var m = _mem.Pop();
			if (m != null)
				return m;

			// No memory, reply with xnone.
			if (_xnone != null)
			{
				Key dummy = null;
				var reply = Decompose(_xnone, input, ref dummy);
				if (reply != null)
					return reply;
			}

			// No xnone, just say anything.
			return "I am at a loss for words.";
		}

	    /// <summary>Break the string s into words.</summary>
	    /// <remarks>
	    /// Break the string s into words.
	    /// For each word, if isKey is true, then push the key
	    /// into the stack.
	    /// </remarks>
	    private void BuildKeyStack([NotNull] List<Key> stack, string s)
	    {
	        stack.Clear();
	        s = s.TrimStart();
	        var lines = new string[2];

	        Key k;
	        while (EString.Match(s, "* *", lines))
	        {
                if (_script.Keys.TryGetValue(lines[0], out k) && k != null)
	                stack.Add(k);

	            s = lines[1];
	        }

            if (_script.Keys.TryGetValue(s, out k) && k != null)
	            stack.Add(k);

	        stack.Sort((a, b) => -a.Rank.CompareTo(b.Rank));
	    }

	    [NotNull] private static string Translate([NotNull] IReadOnlyList<Transform> list, string s)
	    {
	        string Xlate(string str)
	        {
	            return  list.SingleOrDefault(a => a.Source == str)?.Destination ?? str;
	        }

	        var lines = new string[2];
	        var work = s.TrimStart();
	        s = string.Empty;
	        while (EString.Match(work, "* *", lines))
	        {
	            s += Xlate(lines[0]) + " ";
	            work = lines[1].TrimStart();
	        }
	        s += Xlate(work);
	        return s;
	    }

	    /// <summary>Process a sentence.</summary>
		/// <remarks>
		/// Process a sentence. (1) Make pre transformations. (2) Check for quit
		/// word. (3) Scan sentence for keys, build key stack. (4) Try decompositions
		/// for each key.
		/// </remarks>
		private string Sentence(string s)
		{
			s = Translate(_script.Pre, s);
			s = EString.Pad(s);
			if (_script.Quit.Contains(s))
			{
				Finished = true;
			    return _script.Final.Random(new Random(s.GetHashCode()));
			}
		    BuildKeyStack(_keyStack, s);
			for (var i = 0; i < _keyStack.Count; i++)
			{
			    Key gotoKey = null;
				var reply = Decompose(_keyStack[i], s, ref gotoKey);
				if (reply != null)
				{
					return reply;
				}
				// If decomposition returned gotoKey, try it
				while (gotoKey.Keyword != null)
				{
					reply = Decompose(gotoKey, s, ref gotoKey);
					if (reply != null)
					{
						return reply;
					}
				}
			}
			return null;
		}

		/// <summary>Decompose a string according to the given key.</summary>
		/// <remarks>
		/// Decompose a string according to the given key. Try each decomposition
		/// rule in order. If it matches, assemble a reply and return it. If assembly
		/// fails, try another decomposition rule. If assembly is a goto rule, return
		/// null and give the key. If assembly succeeds, return the reply;
		/// </remarks>
		[CanBeNull] private string Decompose([NotNull] Key key, string s, ref Key gotoKey)
		{
		    // Decomposition match, If decomp has no synonyms, do a regular match.
		    bool MatchDecomp(string str, string pat, string[] lines)
		    {
		        if (!EString.Match(pat, "*@* *", lines))
		        {
		            //  no synonyms in decomp pattern
		            return EString.Match(str, pat, lines);
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
		            if (EString.Match(str, pat, lines))
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
				if (MatchDecomp(s, pat, reply))
				{
					var rep = Assemble(d, reply, ref gotoKey);
					if (rep != null)
					{
						return rep;
					}
					if (gotoKey.Keyword != null)
					{
						return null;
					}
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
		[CanBeNull] private string Assemble([NotNull] Decomposition d, string[] reply, ref Key gotoKey)
		{
			var lines = new string[3];
		    var rule = d.Reassemblies.RandomElement(_random);
			if (EString.Match(rule, "goto *", lines))
			{
				// goto rule -- set gotoKey and return false.
			    if (_script.Keys.TryGetValue(lines[0], out gotoKey))
				    if (gotoKey?.Keyword != null)
					    return null;
				return null;
			}
			var work = string.Empty;
			while (EString.Match(rule, "* (#)*", lines))
			{
				// reassembly rule with number substitution
				rule = lines[2];

				// there might be more
			    if (int.TryParse(lines[1], out var n))
				    n--;
                else
				    Console.WriteLine("Number is wrong in reassembly rule " + lines[1]);

				if (n < 0 || n >= reply.Length)
					return null;

				reply[n] = Translate(_script.Post, reply[n]);
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
	}
}
