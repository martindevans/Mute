using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using Mute.Moe.Discord.Services.Responses.Eliza.Scripts;
using Mute.Moe.Discord.Services.Responses.Ellen.Knowledge;

namespace Mute.Moe.Discord.Services.Responses.Ellen.Topics
{
    public class ElizaTopicProviderAdapter
        : ITopicKeyProvider
    {
        private readonly List<ITopicKey> _keys;

        public ElizaTopicProviderAdapter(Script script)
        {
            _keys = new List<ITopicKey>();
            foreach (var (_, values) in script.GetKeys())
                foreach (var value in values)
                    _keys.Add(new ElizaTopicAdapter(value, script));

        }

        public IEnumerable<ITopicKey> Keys => _keys;
    }

    public class ElizaTopicAdapter
        : ITopicKey
    {
        private readonly Script _script;

        public IReadOnlyList<string> Keywords { get; }
        public int Rank { get; }

        public ElizaTopicAdapter(Key key, Script script)
        {
            Rank = key.Rank;
            Keywords = new[] { key.Keyword };
            _script = script;
        }

        public async Task<ITopicDiscussion?> TryBegin(MuteCommandContext message, IKnowledge knowledge, CancellationToken ct)
        {
            return new ElizaKeyDiscussionAdapter(_script);
        }

        private class ElizaKeyDiscussionAdapter
            : ITopicDiscussion
        {
            private readonly Script _script;

            public bool IsComplete { get; private set; }

            public ElizaKeyDiscussionAdapter(Script script)
            {
                _script = script;
            }

            public async Task<(string?, IKnowledge)> Reply(IKnowledge knowledge, MuteCommandContext message)
            {
                if (IsComplete)
                    return (null, knowledge);

                // Get or construct an eliza engine from the knowledge chain
                // If the previous engine has finished a new one will be constructed
                ElizaEngineAdapter engine;
                (engine, knowledge) = knowledge.GetOrAdd(e => !e.Eliza.Finished, k => new ElizaEngineAdapter(k, _script));

                // Get a response from eliza and pass it back
                var response = engine.Eliza.ProcessInput(message);
                IsComplete = engine.Eliza.Finished;
                return (response, knowledge);
            }
        }

        private class ElizaEngineAdapter
            : IKnowledge
        {
            public IKnowledge? Previous { get; }
            public ElizaMain Eliza { get; }

            public ElizaEngineAdapter(IKnowledge? previous, Script script)
            {
                Previous = previous;
                Eliza = new ElizaMain(script, unchecked((int)DateTime.UtcNow.Ticks));
            }
        }
    }
}
