using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Modules
{
    public class BaseModule
        : InteractiveBase
    {
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
            await DisplayItemList(items,
                nothing,
                singleResult,
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
        /// <param name="singleResult">Generate a string for a single item</param>
        /// <param name="fewResults">Generate a string for the given set of results</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="displayItem">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<Task> nothing, Func<T, Task> singleResult, Func<IReadOnlyList<T>, Task> fewResults, Func<IReadOnlyList<T>, Task> manyPrelude, Func<T, int, Task> displayItem)
        {
            if (items.Count == 0)
            {
                await nothing();
                return;
            }

            //Make sure we have a fresh user list to resolve users from IDs
            await Context.Guild.DownloadUsersAsync();

            if (items.Count == 1)
            {
                await singleResult(items.Single());
            }
            else if (items.Count < 5 && fewResults != null)
            {
                await fewResults(items);
            }
            else
            {
                await manyPrelude(items);

                var index = 0;
                foreach (var item in items)
                    await displayItem(item, index++);
            }
        }

        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="itemToString">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<string> nothing, Func<IReadOnlyList<T>, string> manyPrelude, Func<T, int, string> itemToString)
        {
            if (items.Count == 0)
            {
                await this.TypingReplyAsync(nothing());
            }
            else
            {
                //Make sure we have a fresh user list to resolve users from IDs
                await Context.Guild.DownloadUsersAsync();

                await ReplyAsync(manyPrelude(items));

                var index = 0;
                foreach (var item in items)
                    await ReplyAsync(itemToString(item, index++));
            }
        }

        /// <summary>
        /// Display a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="singleResult">Generate a string for a single item</param>
        /// <param name="fewResults">Generate a string for the given set of results</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="itemToString">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        protected async Task DisplayItemList<T>([NotNull] IReadOnlyList<T> items, Func<string> nothing, Func<T, string> singleResult, [CanBeNull] Func<IReadOnlyList<T>, string> fewResults, Func<IReadOnlyList<T>, string> manyPrelude, Func<T, int, string> itemToString)
        {
            await DisplayItemList(
                items,
                async () => await ReplyAsync(nothing()),
                async l => await ReplyAsync(singleResult(items.Single())),
                fewResults != null ? async l => await ReplyAsync(fewResults(items)) : (Func<IReadOnlyList<T>, Task>)null,
                async l => await this.TypingReplyAsync(manyPrelude(items)),
                async (t, i) => await ReplyAsync(itemToString(t, i))
            );
        }
    }
}
