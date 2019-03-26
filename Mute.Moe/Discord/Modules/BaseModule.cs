using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules
{
    public class BaseModule
        : InteractiveBase
    {
        public new MuteCommandContext Context => (MuteCommandContext)base.Context;

        private IEndExecute[] _afterExecuteDisposals;

        #region display lists
        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="displayItem">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<Task> nothing, Func<IReadOnlyList<T>, Task> manyPrelude, Func<T, int, Task> displayItem)
        {
            await DisplayItemList(
                items,
                nothing,
                null,
                manyPrelude,
                displayItem
            );
        }

        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="displayItem">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<Task> nothing, Func<T, int, Task> displayItem)
        {
            await DisplayItemList(
                items,
                nothing,
                null,
                null,
                displayItem
            );
        }

        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="singleResult">Generate a string for a single item</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="displayItem">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<Task> nothing, Func<T, Task> singleResult, Func<IReadOnlyList<T>, Task> manyPrelude, Func<T, int, Task> displayItem)
        {
            if (items.Count == 0)
            {
                await nothing();
                return;
            }

            //Make sure we have a fresh user list to resolve users from IDs
            await Context.Guild.DownloadUsersAsync();

            if (items.Count == 1 && singleResult != null)
            {
                await singleResult(items.Single());
            }
            else
            {
                if (manyPrelude != null)
                    await manyPrelude(items);

                var index = 0;
                foreach (var item in items)
                    await displayItem(item, index++);
            }
        }

        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<string> nothing, Func<IReadOnlyList<T>, string> manyPrelude, Func<T, int, string> itemToString)
        {
            await DisplayItemList(
                items,
                nothing,
                null,
                manyPrelude,
                itemToString
            );
        }

        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="singleItem">Display a summary for a single item</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="itemToString">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<string> nothing, [CanBeNull] Func<T, Task> singleItem, [CanBeNull] Func<IReadOnlyList<T>, string> manyPrelude, Func<T, int, string> itemToString)
        {
            if (items.Count == 0)
            {
                await TypingReplyAsync(nothing());
            }
            else if (items.Count == 1 && singleItem != null)
            {
                await singleItem(items[0]);
            }
            else
            {
                //Make sure we have a fresh user list to resolve users from IDs
                await Context.Guild.DownloadUsersAsync();

                var p = manyPrelude?.Invoke(items);
                if (p != null)
                    await ReplyAsync(p);

                var builder = new StringBuilder();

                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var str = itemToString(item, i);

                    if (builder.Length + str.Length > 1000)
                    {
                        await ReplyAsync(builder.ToString());
                        builder.Clear();
                    }

                    builder.Append(str);
                    builder.Append('\n');
                }

                if (builder.Length > 0)
                    await ReplyAsync(builder.ToString());
            }
        }
        #endregion

        #region reply
        protected async Task<IUserMessage> TypingReplyAsync([NotNull] string message, bool isTTS = false, [CanBeNull] Embed embed = null, [CanBeNull] RequestOptions options = null)
        {
            return await Context.Channel.TypingReplyAsync(message, isTTS, embed, options);
        }

        protected async Task<IUserMessage> TypingReplyAsync([NotNull] EmbedBuilder embed, [CanBeNull] RequestOptions options = null)
        {
            return await TypingReplyAsync("", false, embed.Build(), options);
        }

        protected async Task<IUserMessage> ReplyAsync([NotNull] EmbedBuilder embed, [CanBeNull] RequestOptions options = null)
        {
            return await ReplyAsync("", false, embed.Build(), options);
        }
        #endregion

        #region user names
        public string Name(ulong id, bool mention = false)
        {
            var user = Context.Client.GetUser(id);
            if (user == null)
                return $"UNKNOWN_USER:{id}";

            return Name(user, mention);
        }

        public static string Name([NotNull] IUser user, bool mention = false)
        {
            if (mention)
                return user.Mention;

            return (user as IGuildUser)?.Nickname ?? user.Username;
        }
        #endregion

        protected override void BeforeExecute([NotNull] CommandInfo command)
        {
            var method = command.Attributes.OfType<BaseExecuteContextAttribute>();
            var module = GetType().GetCustomAttributes(typeof(BaseExecuteContextAttribute), true).Cast<BaseExecuteContextAttribute>();
            _afterExecuteDisposals = method.Concat(module).Select(eca => eca.StartExecute(Context)).ToArray();

            base.BeforeExecute(command);
        }

        protected override void AfterExecute(CommandInfo command)
        {
            foreach (var item in _afterExecuteDisposals)
                item.EndExecute().GetAwaiter().GetResult();

            base.AfterExecute(command);
        }
    }
}
