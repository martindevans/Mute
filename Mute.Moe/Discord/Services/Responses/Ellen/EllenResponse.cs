using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq.Extensions;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Services.Responses.Ellen.Knowledge;
using Mute.Moe.Discord.Services.Responses.Ellen.Topics;

namespace Mute.Moe.Discord.Services.Responses.Ellen
{
    public class EllenResponse
        : IResponse
    {
        private readonly IReadOnlyList<ITopicKey> _topics;

        public double BaseChance => 0;
        public double MentionedChance => 0;

        public EllenResponse(IServiceProvider services)
        {
            //Get topics
            _topics = (from t in Assembly.GetExecutingAssembly().GetTypes()
                       where t.IsClass
                       where typeof(ITopicKeyProvider).IsAssignableFrom(t)
                       let i = ActivatorUtilities.CreateInstance(services, t) as ITopicKeyProvider
                       where i != null
                       from k in i.Keys
                       orderby k.Rank
                       select k).ToArray();
        }

        public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
        {
            return new EllenConversation(_topics, new Root());
        }

        private class EllenConversation
            : IConversation
        {
            private readonly IReadOnlyList<ITopicKey> _topics;

            private ITopicDiscussion? _active;
            private IKnowledge _knowledge;

            public bool IsComplete { get; private set; }

            public EllenConversation(IReadOnlyList<ITopicKey> topics, IKnowledge root)
            {
                _topics = topics;
                _knowledge = root;
            }

            public async Task<string?> Respond(MuteCommandContext message, bool containsMention, CancellationToken ct)
            {
                async Task<string?> ContinueActiveDiscussion()
                {
                    // if there is an active discussion, try to continue it
                    if (_active != null && !_active.IsComplete)
                    {
                        string? reply;
                        (reply, _knowledge) = await _active.Reply(_knowledge, message);

                        return reply;
                    }

                    return null;
                }

                // Try to coninue the active discussion
                var reply = await ContinueActiveDiscussion();
                if (reply != null)
                    return reply;

                // Either there was no active discussion, or the active discussion didn't produce a reply. Try to
                // start a new discussion from all relevant topics. Relevant topics with the same rank are shuffled.
                var topics = from t in _topics.AsParallel()
                             where IsRelevant(t, message)
                             group t by t.Rank into g
                             orderby g.Key
                             from t in g.Shuffle()
                             select t;

                // Try to start a conversation replying to every topic (each topic gets it's own task)
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var starters = (from t in topics
                                let task = t.TryBegin(message, _knowledge, cts.Token)
                                select task).ToArray();

                // Find the first (highest ranked) task which produced a useful result
                foreach (var task in starters)
                {
                    var discussion = await task;
                    if (discussion != null && !discussion.IsComplete)
                    {
                        // Try to use this discussion
                        _active = discussion;
                        reply = await ContinueActiveDiscussion();
                        if (reply != null)
                        {
                            // Successfully generated a response! cancel all the other tasks
                            cts.Cancel();
                            return reply;
                        }
                    }
                }

                // None of the topics produced a result, end the conversation
                IsComplete = true;
                return null;
            }

            private static bool IsRelevant(ITopicKey key, MuteCommandContext context)
            {
                foreach (var keyword in key.Keywords)
                    if (context.Message.Content.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                        return true;

                return false;
            }
        }
    }
}
