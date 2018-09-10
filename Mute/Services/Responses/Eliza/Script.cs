using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Mute.Services.Responses.Eliza.Eliza;

namespace Mute.Services.Responses.Eliza
{
    public class Script
    {
        public IReadOnlyDictionary<string, Key> Keys { get; }

        private readonly List<IReadOnlyList<string>> _syns = new List<IReadOnlyList<string>>();
        public IReadOnlyList<IReadOnlyList<string>> Syns => _syns;

        public IReadOnlyDictionary<string, Transform> Pre { get; }

        public IReadOnlyDictionary<string, Transform> Post { get; }

        private readonly List<string> _final = new List<string>();
        public IReadOnlyList<string> Final => _final;

        public IReadOnlyList<string> Quit { get; }

        public Script([NotNull] IEnumerable<string> lines)
        {
            List<Decomposition> lastDecomp = null;
            List<string> lastReasemb = null;
            var keysList = new List<Key>();
            var pre = new List<Transform>();
            var post = new List<Transform>();
            var quit = new List<string>();

            foreach (var line in lines)    
                Collect(line, ref lastReasemb, ref lastDecomp, keysList, pre, post, quit);

            Keys = new ReadOnlyDictionary<string, Key>(keysList.ToDictionary(a => a.Keyword, a => a));
            Pre = new ReadOnlyDictionary<string, Transform>(pre.ToDictionary(a => a.Source, a => a));
            Post = new ReadOnlyDictionary<string, Transform>(post.ToDictionary(a => a.Source, a => a));
            Quit = quit;
        }

        /// <summary>Process a line of script input.</summary>
		/// <remarks>Process a line of script input.</remarks>
		private void Collect(string s, ref List<string> lastReasemb, ref List<Decomposition> lastDecomp, ICollection<Key> keys, ICollection<Transform> pre, ICollection<Transform> post, ICollection<string> quit)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;

			var lines = new string[4];
			if (EString.Match(s, "*reasmb: *", lines))
			{
				if (lastReasemb == null)
				{
					return;
				}
				lastReasemb.Add(lines[1]);
			}
			else
			{
				if (EString.Match(s, "*decomp: *", lines))
				{
					if (lastDecomp == null)
					{
						return;
					}
				    lastReasemb = new List<string>();
					string temp = lines[1];
					if (EString.Match(temp, "$ *", lines))
					{
					    lastDecomp.Add(new Decomposition(lines[0], true, lastReasemb));
					}
					else
					{
					    lastDecomp.Add(new Decomposition(temp, false, lastReasemb));
					}
				}
				else
				{
					if (EString.Match(s, "*key: * #*", lines))
					{
						lastDecomp = new List<Decomposition>();
						lastReasemb = null;
						int n = 0;
						if (lines[2].Length != 0)
						{
							try
							{
								n = int.Parse(lines[2]);
							}
							catch (FormatException)
							{
								Console.WriteLine("Number is wrong in key: " + lines[2]);
							}
						}
					    keys.Add(new Key(lines[1], n, lastDecomp));
					}
					else
					{
						if (EString.Match(s, "*key: *", lines))
						{
							lastDecomp = new List<Decomposition>();
							lastReasemb = null;
						    keys.Add(new Key(lines[1], 0, lastDecomp));
						}
						else
						{
							if (EString.Match(s, "*synon: * *", lines))
							{
								var words = new List<string>();
								words.Add(lines[1]);
								s = lines[2];
								while (EString.Match(s, "* *", lines))
								{
									words.Add(lines[0]);
									s = lines[1];
								}
								words.Add(s);
							    _syns.Add(words);
							}
							else
							{
								if (EString.Match(s, "*pre: * *", lines))
								{
								    pre.Add(new Transform(lines[1], lines[2]));
								}
								else
								{
									if (EString.Match(s, "*post: * *", lines))
									{
									    post.Add(new Transform(lines[1], lines[2]));
									}
									else
									{
										if (EString.Match(s, "*final: *", lines))
										{
										    _final.Add(lines[1]);
										}
										else
										{
											if (EString.Match(s, "*quit: *", lines))
											{
												quit.Add(lines[1]);
											}
											else
											{
												Console.WriteLine("Unrecognized input: " + s);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
    }
}
