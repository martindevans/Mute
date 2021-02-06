using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using Mute.Moe.Discord.Services.Responses.Eliza.Scripts;

namespace Mute.Moe.Discord.Services.Responses.Eliza
{
    public class ElizaResponse
        : IResponse
    {
        public double BaseChance => 0.0;
        public double MentionedChance => 0.99;

        private readonly List<string> _greetings = new() {
            "hello", "hi", "hiya", "heya", "howdy"
        };

        private readonly Script _script;

        public ElizaResponse(Script script)
        {
            _script = script;
        }

        public Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
        {
            return Task.Run<IConversation?>(() =>
            {

                //Determine if this message is a greeting
                var isGreeting = context.Message.Content.Split(' ').Select(CleanWord).Any(_greetings.Contains);

                if (isGreeting)
                {
                    var seed = context.Message.Id.GetHashCode();
                    return new ElizaConversation(_script, seed);
                }
                else
                    return null;

            });
        }

        private static string CleanWord(string word)
        {
            return new(word
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

            public Task<string?> Respond(MuteCommandContext context, bool containsMention, CancellationToken ct)
            {
                return Task.Run<string?>(() =>
                {
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
