using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Context;
using Mute.Services.Responses.Eliza.Engine;
using Mute.Services.Responses.Eliza.Scripts;
using IEnumerableExtensions = Mute.Extensions.IEnumerableExtensions;

namespace Mute.Services.Responses.Eliza
{
    public class ElizaResponse
        : IResponse
    {
        public double BaseChance => 0.0;
        public double MentionedChance => 0.9;

        private readonly List<string> _greetings = new List<string> {
            "hello", "hi", "hiya", "heya", "howdy"
        };

        private readonly IReadOnlyList<Script> _scripts;

        //private readonly IReadOnlyList<ITopic> _topics;

        public ElizaResponse([NotNull] Configuration config, IServiceProvider services)
        {
            ////Get topics
            //_topics = (from t in Assembly.GetExecutingAssembly().GetTypes()
            //           where t.IsClass
            //           where typeof(ITopic).IsAssignableFrom(t)
            //           let i = ActivatorUtilities.CreateInstance(services, t) as ITopic
            //           where i != null
            //           select i).ToArray();

            //Get basic key providers
            var keys = (from t in Assembly.GetExecutingAssembly().GetTypes()
                        where t.IsClass
                        where typeof(IKeyProvider).IsAssignableFrom(t)
                        let kp = ActivatorUtilities.CreateInstance(services, t) as IKeyProvider
                        where kp != null
                       select kp).ToArray();

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
                    scripts.Add(new Script(txt, keys));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Encountered exception {e} trying to read Eliza script {path}");
                }
                _scripts = scripts;
            }
        }

        public Task<IConversation> TryRespond(MuteCommandContext context, bool containsMention)
        {
            return Task.Run<IConversation>(() => {

                if (_scripts.Count == 0)
                    return null;

                //Determine if thie message is a greeting
                var isGreeting = context.Message.Content.Split(' ').Select(CleanWord).Any(_greetings.Contains);

                if (isGreeting)
                {
                    var seed = context.Message.Id.GetHashCode();
                    var rand = new Random(seed);
                    return new ElizaConversation(IEnumerableExtensions.Random(_scripts, rand), seed);
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

            public Task<string> Respond(MuteCommandContext context, bool containsMention, CancellationToken ct)
            {
                return Task.Run(() => {
                    lock (_eliza)
                    {
                        var response = _eliza.ProcessInput(context);
                        IsComplete = _eliza.Finished;
                        return response;
                    }
                }, ct);
            }

            public override string ToString()
            {
                lock (_eliza)
                {
                    return _eliza.ToString();
                }
            }
        }
    }
}
