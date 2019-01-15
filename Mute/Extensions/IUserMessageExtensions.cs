using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;

namespace Mute.Extensions
{
    public static class IUserMessageExtensions
    {
        /// <summary>
        /// Add a :thinking: emoji to a message for the duration of a task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="bot"></param>
        /// <param name="work"></param>
        /// <param name="emote"></param>
        /// <returns></returns>
        public static async Task<T> ThinkingReplyAsync<T>([NotNull] this IUserMessage message, [NotNull] IUser bot, [NotNull] Task<T> work, [CanBeNull] Emoji emote = null)
        {
            emote = emote ?? EmojiLookup.Thinking;

            using (message.Channel.EnterTypingState())
            {
                // Add a thinking emoji but do not wait for it to be added
                var emoji = message.AddReactionAsync(emote);

                // Do the work
                var result = await work;

                // Wait for the emoji to be added so that we can safely remove it
                await emoji;

                //Remove the thinking emoji
                await message.RemoveReactionAsync(emote, bot);

                return result;
            }
        }

        [ItemCanBeNull] private static async Task<object> VoidTask([NotNull] Task task)
        {
            await task;
            return null;
        }

        public static async Task ThinkingReplyAsync([NotNull] this IUserMessage message, [NotNull] IUser bot, [NotNull] Task work, [CanBeNull] Emoji emote = null)
        {
            await ThinkingReplyAsync(message, bot, VoidTask(work), emote);
        }
    }
}
