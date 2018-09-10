using System.Threading;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;

namespace Mute.Services.Responses
{
    public class TerminalConversation
        : IConversation
    {
        private readonly string _response;
        private readonly IEmote[] _reactions;

        public bool IsComplete { get; private set; }

        public TerminalConversation(string response, [CanBeNull] params IEmote[] reactions)
        {
            _response = response;
            _reactions = reactions;
        }

        public Task<string> Respond(IMessage message, bool containsMention, CancellationToken ct)
        {
            IsComplete = true;

            if (_reactions != null && _reactions.Length > 0 && message is IUserMessage umsg)
            {
                Task.Run(async () => {
                    foreach (var reaction in _reactions)
                        await umsg.AddReactionAsync(reaction);
                }, ct);
            }

            return Task.FromResult(_response);
        }
    }
}
