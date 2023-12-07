using Discord.Interactions;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Discord.Interactions
{
    public class MuteInteractionModuleBase
        : InteractionModuleBase
    {
        #region send reply
        protected async Task ReplyAsyc(string? text = null, Embed[]? embeds = null, bool ephemeral = false)
        {
            if (!Context.Interaction.HasResponded)
                await RespondAsync(text, embeds: embeds, ephemeral: ephemeral);
            else
                await FollowupAsync(text, embeds: embeds, ephemeral: ephemeral);
        }
        #endregion

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
        protected Task DisplayItemList<T>(IReadOnlyList<T> items, Func<Task> nothing, Func<IReadOnlyList<T>, Task> manyPrelude, Func<T, int, Task> displayItem)
        {
            return DisplayItemList(
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
        protected Task DisplayItemList<T>(IReadOnlyList<T> items, Func<Task> nothing, Func<T, int, Task> displayItem)
        {
            return DisplayItemList(
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
        protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<Task> nothing, Func<T, Task>? singleResult, Func<IReadOnlyList<T>, Task>? manyPrelude, Func<T, int, Task> displayItem)
        {
            switch (items.Count)
            {
                case 0:
                    await nothing();
                    return;

                case 1 when singleResult != null:
                    await singleResult(items.Single());
                    break;

                default:
                    {
                        if (manyPrelude != null)
                            await manyPrelude(items);

                        var index = 0;
                        foreach (var item in items)
                            await displayItem(item, index++);
                        break;
                    }
            }
        }

        protected Task DisplayItemList<T>(IReadOnlyList<T> items, Func<string> nothing, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
        {
            return DisplayItemList(
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
        protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<string> nothing, Func<T, string>? singleItem, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
        {
            switch (items.Count)
            {
                case 0:
                    await ReplyAsyc(nothing());
                    break;

                case 1 when singleItem != null:
                    await ReplyAsyc(singleItem(items[0]));
                    break;

                default:
                    {
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
                        break;
                    }
            }
        }

        protected Task DisplayItemList<T>(IReadOnlyList<T> items, string nothing, Func<T, string>? singleItem, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
        {
            return DisplayItemList(items, () => nothing, singleItem, manyPrelude, itemToString);
        }
        #endregion
    }
}
