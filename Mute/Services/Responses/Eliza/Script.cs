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

        /// <summary>The syn list</summary>
        private readonly List<IReadOnlyList<string>> _syns = new List<IReadOnlyList<string>>();
        public IReadOnlyList<IReadOnlyList<string>> Syns => _syns;

        /// <summary>The pre list</summary>
        private readonly List<Transform> _pre = new List<Transform>();
        public IReadOnlyList<Transform> Pre => _pre;

        /// <summary>The post list</summary>
        private readonly List<Transform> _post = new List<Transform>();
        public IReadOnlyList<Transform> Post => _post;

        /// <summary>Final string</summary>
        private readonly List<string> _final = new List<string>();
        public IReadOnlyList<string> Final => _final;

        /// <summary>Quit list</summary>
        private readonly List<string> _quit = new List<string>();

        
        public IReadOnlyList<string> Quit => _quit;

        public Script([NotNull] IEnumerable<string> lines)
        {
            List<Decomposition> lastDecomp = null;
            List<string> lastReasemb = null;
            var keysList = new List<Key>();

            foreach (var line in lines)    
                Collect(line, ref lastReasemb, ref lastDecomp, keysList);

            Keys = new ReadOnlyDictionary<string, Key>(keysList.ToDictionary(a => a.Keyword, a => a));
        }

        /// <summary>Process a line of script input.</summary>
		/// <remarks>Process a line of script input.</remarks>
		private void Collect(string s, ref List<string> lastReasemb, ref List<Decomposition> lastDecomp, List<Key> keys)
		{
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
								    _pre.Add(new Transform(lines[1], lines[2]));
								}
								else
								{
									if (EString.Match(s, "*post: * *", lines))
									{
									    _post.Add(new Transform(lines[1], lines[2]));
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
												_quit.Add(" " + lines[1] + " ");
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
