using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class ModuleBaseExtensions
    {
        public static async Task<IUserMessage> TypingReplyAsync([NotNull] this ModuleBase module, [NotNull] string message, bool isTTS = false, [CanBeNull] Embed embed = null, [CanBeNull] RequestOptions options = null)
        {
            return await module.Context.Channel.TypingReplyAsync(message, isTTS, embed, options);
        }

        public static async Task<IUserMessage> TypingReplyAsync([NotNull] this InteractiveBase module, [NotNull] string message, bool isTTS = false, [CanBeNull] Embed embed = null, [CanBeNull] RequestOptions options = null)
        {
            return await module.Context.Channel.TypingReplyAsync(message, isTTS, embed, options);
        }
    }
}
