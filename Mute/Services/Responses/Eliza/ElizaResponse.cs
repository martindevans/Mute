using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services.Responses.Eliza.Eliza;

namespace Mute.Services.Responses.Eliza
{
    public class ElizaResponse
        : IResponse
    {
        public double BaseChance => 10.15;
        public double MentionedChance => 1;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy"
        };

        private readonly IReadOnlyList<Script> _scripts;

        public ElizaResponse([NotNull] Configuration config)
        {
            foreach (var path in config.ElizaConfig.Scripts)
            {
                if (!File.Exists(path))
                    continue;

                var txt = File.ReadAllLines(path);
                if (txt == null || txt.Length == 0)
                    continue;

                var scripts = new List<Script>();
                try
                {
                    scripts.Add(new Script(txt));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Encountered exception {e} trying to read Eliza script {path}");
                }
                _scripts = scripts;
            }
        }

        public Task<IConversation> TryRespond(IMessage message, bool containsMention)
        {
            return Task.Run<IConversation>(() => {

                if (_scripts.Count == 0)
                    return null;

                //Determine if thie message is a greeting
                var isGreeting = message.Content.Split(' ').Select(CleanWord).Any(_greetings.Contains);

                if (isGreeting)
                {
                    var seed = message.CreatedAt.UtcTicks.GetHashCode();
                    var rand = new Random(seed);
                    return new ElizaConversation(_scripts.RandomElement(rand), seed);
                }
                else
                    return null;

            });
        }

        [NotNull] private static string CleanWord([NotNull] string word)
        {
            return new string(word
                .ToLowerInvariant()
                .Where(c => !char.IsPunctuation(c))
                .ToArray()
            );
        }

        private class ElizaConversation
            : IConversation
        {
            private readonly ElizaMain _eliza;

            public ElizaConversation(Script script, int seed)
            {
                _eliza = new ElizaMain(script, seed);
            }

            public bool IsComplete { get; private set; }

            public Task<string> Respond(IMessage message, bool containsMention, CancellationToken ct)
            {
                var response = _eliza.ProcessInput(message.Content);
                IsComplete = _eliza.Finished;
                return Task.FromResult(response);
            }
        }
    }
}
