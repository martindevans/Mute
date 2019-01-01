using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Services;

namespace Mute.Modules
{
    [Group("chatlog")]
    public class ChatLogging
        : BaseModule
    {
        private readonly HistoryLoggingService _history;

        public ChatLogging(HistoryLoggingService history)
        {
            _history = history;
        }

        [RequireOwner, Command("subscribe"), Summary("I will begin logging all messages in the given channel")]
        public async Task Subscribe([NotNull] ITextChannel channel)
        {
            await _history.BeginMonitoring(channel);
        }

        [RequireOwner, Command("list"), Summary("I will list all current channel monitoring subscriptions")]
        public async Task ListSubscriptions()
        {
            var list = await _history.GetSubscriptions();
            await DisplayItemList(list,
                () => "Not logging any channels",
                l => $"Currently monitoring {l.Count} channels",
                (item, index) => $"`#{item.Item2.Name}` in `{item.Item1.Name}`"
            );
        }
    }
}
