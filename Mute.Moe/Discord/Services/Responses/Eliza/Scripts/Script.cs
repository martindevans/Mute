using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;

namespace Mute.Moe.Discord.Services.Responses.Eliza.Scripts
{
    public class Script
    {
        public string Name { get; }

        private readonly IReadOnlyDictionary<string, IReadOnlyList<Key>> _keys;

        public IReadOnlyList<IReadOnlyCollection<string>> Synonyms { get; }
        public IReadOnlyDictionary<string, Transform> InputTransforms { get; }
        public IReadOnlyDictionary<string, Transform> OutputTransforms { get; }
        public IReadOnlyList<string> Final { get; }
        public IReadOnlyList<string> Quit { get; }

        public Script(string name, IEnumerable<string> lines, IEnumerable<IKeyProvider> keyProviders)
        {
            Name = name;

            List<Decomposition>? lastDecomp = null;
            List<IReassembly>? lastReasemb = null;
            var keysList = new List<Key>();
            var pre = new List<Transform>();
            var post = new List<Transform>();
            var quit = new List<string>();
            var syns = new List<HashSet<string>>();
            var final = new List<string>();

            //Parse all the lines of the script
            foreach (var line in lines)    
                ParseLine(line, ref lastReasemb, ref lastDecomp, keysList, pre, post, quit, syns, final);

            Synonyms = syns;
            InputTransforms = new ReadOnlyDictionary<string, Transform>(pre.ToDictionary(a => a.Source, a => a));
            OutputTransforms = new ReadOnlyDictionary<string, Transform>(post.ToDictionary(a => a.Source, a => a));
            Quit = quit;
            Final = final;

            // Get keys from all external providers
            keysList.AddRange(keyProviders.SelectMany(kp => kp.Keys));

            // Expand `@foo` in the keyword position into multiple rules, one for each synonym of `foo`
            ExpandSynonymKeys(keysList, syns);

            _keys = new ReadOnlyDictionary<string, IReadOnlyList<Key>>((
                from key in keysList
                group key by key.Keyword
                into kgroup
                select kgroup).ToDictionary(a => a.Key, a => (IReadOnlyList<Key>)a.ToArray()
            ));
        }

        public IReadOnlyDictionary<string, IReadOnlyList<Key>> GetKeys()
        {
            return _keys;
        }

        public IEnumerable<Key> GetKeys(string keyword)
        {
            //Get keys from script
            if (!_keys.TryGetValue(keyword, out var results))
                return Array.Empty<Key>();
            else
                return results;
        }

        #region parsing
        private static void ExpandSynonymKeys(IList<Key> keysList, IReadOnlyList<IReadOnlyCollection<string>> syns)
        {
            for (var i = keysList.Count - 1; i >= 0; i--)
            {
                //Find out if this key uses a synonym as it's keyword...
                var k = keysList[i];
                if (!keysList[i].Keyword.StartsWith('@'))
                    continue;

                //...It does, so remove it
                keysList.RemoveAt(i);

                //Find synonyms of keyword
                var kw = k.Keyword.Substring(1);
                var synonyms = syns.FirstOrDefault(a => a.Contains(kw));
                if (synonyms == null)
                    continue;

                //create concrete keys, one for each synonym
                foreach (var synonym in synonyms)
                    keysList.Add(new Key(synonym, k.Rank, k.Decompositions));
            }
        }

        private static bool DecompositionRule(string s, ref List<Decomposition>? lastDecomp, ref List<IReassembly>? lastReasemb)
        {
            var m = Regex.Match(s, "^.*?decomp:(?<modifiers>[~\\$ ]*)(?<value>.*)$");
            if (!m.Success)
                return false;

            if (lastDecomp != null)
            {
                lastReasemb = new List<IReassembly>();

                var val = m.Groups["value"].Value;
                var mod = m.Groups["modifiers"].Value;

                lastDecomp.Add(new Decomposition(val, mod.Contains('$'), mod.Contains('~'), lastReasemb));
            }

            return true;
        }

        private static bool ReassemblyRule(string s, ICollection<IReassembly>? lr)
        {
            var m = Regex.Match(s, "^.*?reasmb:( )+(?<value>.*)$");
            if (m.Success)
            {
                lr?.Add(new ConstantReassembly(m.Groups["value"].Value));
                return true;
            }

            return false;
        }

        private static bool KeysRule(string s, ICollection<Key> keys, ref List<Decomposition>? lastDecomp, ref List<IReassembly>? lastReasemb)
        {
            var m = Regex.Match(s, "^.*?key:( )+(?<value>.*?)( )*(?<rank>\\d*)$");
            if (!m.Success)
                return false;

            var val = m.Groups["value"].Value;
            var rank = m.Groups["rank"].Value;

            int.TryParse(rank, out var rankValue);

            lastDecomp = new List<Decomposition>();
            lastReasemb = null;

            keys.Add(new Key(val, rankValue, lastDecomp));

            return true;
        }

        private static bool SynonymRule(string s, ICollection<HashSet<string>> synonyms)
        {
            var m = Regex.Match(s, "^.*?synon:( )+(?<value>.*)$");
            if (!m.Success)
                return false;

            synonyms.Add(new HashSet<string>(m.Groups["value"].Value.Split(' ')));
            return true;
        }

        private static bool PreRule(string s, ICollection<Transform> pre)
        {
            var m = Regex.Match(s, "^.*?pre:( )+(?<a>.*?)( )+(?<b>.*?)$");
            if (!m.Success)
                return false;

            pre.Add(new Transform(m.Groups["a"].Value, m.Groups["b"].Value));
            return true;
        }

        private static bool PostRule(string s, ICollection<Transform> post)
        {
            var m = Regex.Match(s, "^.*?post:( )+(?<a>.*?)( )+(?<b>.*?)$");
            if (!m.Success)
                return false;

            post.Add(new Transform(m.Groups["a"].Value, m.Groups["b"].Value));
            return true;
        }

        private static bool FinalRule(string s, ICollection<string> final)
        {
            var m = Regex.Match(s, "^.*?final:( )+(?<value>.*)$");
            if (!m.Success)
                return false;

            final.Add(m.Groups["value"].Value);
            return true;
        }

        private static bool QuitRule(string s, ICollection<string> quit)
        {
            var m = Regex.Match(s, "^.*?quit:( )+(?<value>.*)$");
            if (!m.Success)
                return false;

            quit.Add(m.Groups["value"].Value);
            return true;
        }

        /// <summary>Process a line of script input.</summary>
		/// <remarks>Process a line of script input.</remarks>
		private static bool ParseLine(string? line, ref List<IReassembly>? lastReasemb, ref List<Decomposition>? lastDecomp, ICollection<Key> keys, ICollection<Transform> pre, ICollection<Transform> post, ICollection<string> quit, ICollection<HashSet<string>> syns, ICollection<string> final)
        {
            if (line == null || string.IsNullOrWhiteSpace(line))
                return false;

            return ReassemblyRule(line, lastReasemb)
                   || DecompositionRule(line, ref lastDecomp, ref lastReasemb)
                   || KeysRule(line, keys, ref lastDecomp, ref lastReasemb)
                   || SynonymRule(line, syns)
                   || PreRule(line, pre)
                   || PostRule(line, post)
                   || FinalRule(line, final)
                   || QuitRule(line, quit);
		}
        #endregion

        public string TransformOutput(string sentence)
        {
            return Transform(OutputTransforms, sentence);
        }

        public string TransformInput(string sentence)
        {
            return Transform(InputTransforms, sentence);
        }

        /// <summary>
        /// Apply a set of transformation rules to all the words in a sentence
        /// </summary>
        /// <param name="transformations"></param>
        /// <param name="sentence"></param>
        /// <returns></returns>
        private static string Transform(IReadOnlyDictionary<string, Transform> transformations, string sentence)
        {
            var txs = from word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      let tx = transformations.GetValueOrDefault(word)
                      select (word, tx);

            var result = from tx in txs
                         select tx.tx?.Destination ?? tx.word;

            return string.Join(" ", result);
        }

        public static Script Load(IServiceProvider services)
        {
            var config = (Configuration)services.GetService(typeof(Configuration));

            //Get basic key providers
            var keys = (from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.IsClass
                        where typeof(IKeyProvider).IsAssignableFrom(t)
                        let kp = ActivatorUtilities.CreateInstance(services, t) as IKeyProvider
                        where kp != null
                        select kp).ToArray();

            var nullScript = new Script("null", Array.Empty<string>(), Array.Empty<IKeyProvider>());

            // Early exit if script path is missing
            var path = config.ElizaConfig?.Script;
            if (path == null || !File.Exists(path))
                return nullScript;

            // Early exit if script is blank
            var txt = File.ReadAllLines(path);
            if (txt == null || txt.Length == 0)
                return nullScript;

            // Parse the script
            try
            {
                return new Script(Path.GetFileName(path), txt, keys);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Encountered exception {e} trying to read Eliza script {path}");
                return nullScript;
            }
        }
    }
}
