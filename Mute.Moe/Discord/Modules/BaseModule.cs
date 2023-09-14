using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules;

public class BaseModule
    : InteractiveBase
{
    public new MuteCommandContext Context => (MuteCommandContext)base.Context;

    private IEndExecute[]? _afterExecuteDisposals;

    #region pagination
    private class LazyList<T>
    {
        private readonly IAsyncEnumerator<T> _itemsSource;
        private readonly List<T> _cache = new();

        public int CurrentCount => _cache.Count;
        public bool Finished { get; private set; }
        private bool _started;

        public T CurrentItem => _cache[CurrentIndex];
        public int CurrentIndex { get; private set; }

        public LazyList(IAsyncEnumerable<T> items)
        {
            _itemsSource = items.GetAsyncEnumerator();
        }

        public async Task Start()
        {
            if (_started)
                return;
            _started = true;

            Finished = !await _itemsSource.MoveNextAsync();
            await ExpandCache();
        }

        private async Task ExpandCache()
        {
            await Start();

            if (!Finished)
                _cache.Add(_itemsSource.Current);

            Finished = !await _itemsSource.MoveNextAsync();
            if (Finished)
                await _itemsSource.DisposeAsync();
        }

        public async Task GotoStart()
        {
            await Start();

            CurrentIndex = 0;
        }

        public async Task<bool> MoveForward()
        {
            await Start();

            if (CurrentIndex == CurrentCount - 1)
            {
                if (Finished)
                    return false;
                await ExpandCache();
            }

            CurrentIndex++;
            return true;
        }

        public async Task<bool> MoveBackward()
        {
            await Start();

            if (CurrentIndex <= 0)
                return false;

            CurrentIndex--;
            return true;
        }
    }

    private class LazyPaginatedMessage<T>
        : IReactionCallback, ICriterion<SocketReaction>
    {
        public const string SkipBackward = EmojiLookup.SkipBackward;
        public const string MoveBackward = EmojiLookup.FastBackward;
        public const string MoveForward = EmojiLookup.FastForward;

        private readonly IUserMessage _message;
        private readonly LazyList<T> _items;
        private readonly string _title;
        private readonly Action<T, EmbedBuilder> _build;

        public RunMode RunMode => RunMode.Async;
        public ICriterion<SocketReaction> Criterion => this;
        public TimeSpan? Timeout { get; }
        public SocketCommandContext Context { get; }

        public LazyPaginatedMessage(IUserMessage message, LazyList<T> items, TimeSpan timeout, SocketCommandContext context, string title, Action<T, EmbedBuilder> build)
        {
            _message = message;
            _items = items;
            _title = title;
            _build = build;

            Timeout = timeout;
            Context = context;
        }

        private EmbedBuilder BuildEmbed()
        {
            var builder = new EmbedBuilder()
                .WithTitle(_title);

            _build(_items.CurrentItem!, builder);

            builder.WithFooter(
                _items.Finished
                    ? $"Page {_items.CurrentIndex + 1}/{_items.CurrentCount}"
                    : $"Click the reactions to change pages. Page {_items.CurrentIndex + 1}"
            );

            return builder;
        }

        public async Task Draw()
        {
            await _message.ModifyAsync(a => {
                a.Embed = BuildEmbed().Build();
                a.Content = null;
            });
        }

        public async Task<bool> HandleCallbackAsync(SocketReaction reaction)
        {
            // Remove the reaction so the button can be pressed again
            await _message.RemoveReactionAsync(reaction.Emote, reaction.UserId);

            var redraw = false;
            switch (reaction.Emote.Name)
            {
                case SkipBackward:
                    await _items.GotoStart();
                    redraw = true;
                    break;
                case MoveBackward:
                    redraw = await _items.MoveBackward();
                    break;
                case MoveForward:
                    redraw = await _items.MoveForward();
                    break;
            }

            if (redraw)
                await Draw();

            return false;
        }

        Task<bool> ICriterion<SocketReaction>.JudgeAsync(SocketCommandContext sourceContext, SocketReaction parameter) => Task.FromResult(parameter.UserId == sourceContext.User.Id);
    }

    protected async Task DisplayLazyPaginatedReply<T>(string title, IEnumerable<T> pagesEnumerable, Action<T, EmbedBuilder>? build = null)
    {
        await DisplayLazyPaginatedReply(title, pagesEnumerable.ToAsyncEnumerable());
    }

    protected async Task DisplayLazyPaginatedReply<T>(string title, IAsyncEnumerable<T> pagesEnumerable, Action<T, EmbedBuilder>? build = null)
    {
        build ??= (i, b) => b.Description = i?.ToString() ?? "";

        var items = new LazyList<T>(pagesEnumerable);
        await items.Start();

        if (items is { CurrentCount: 0, Finished: true })
            return;

        var msg = await ReplyAsync("Building Paginator...");

        await msg.AddReactionAsync(new Emoji(LazyPaginatedMessage<string>.SkipBackward));
        await msg.AddReactionAsync(new Emoji(LazyPaginatedMessage<string>.MoveBackward));
        await msg.AddReactionAsync(new Emoji(LazyPaginatedMessage<string>.MoveForward));

        var pager = new LazyPaginatedMessage<T>(msg, items, TimeSpan.FromMinutes(5), Context, title, build);
        await pager.Draw();

        Interactive.AddReactionCallback(msg, pager);
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
    protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<Task> nothing, Func<IReadOnlyList<T>, Task> manyPrelude, Func<T, int, Task> displayItem)
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
    protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<Task> nothing, Func<T, int, Task> displayItem)
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

    protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<string> nothing, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
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
    protected async Task DisplayItemList<T>(IReadOnlyList<T> items, Func<string> nothing, Func<T, Task>? singleItem, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
    {
        switch (items.Count)
        {
            case 0:
                await TypingReplyAsync(nothing());
                break;

            case 1 when singleItem != null:
                await singleItem(items[0]);
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

    protected async Task DisplayItemList<T>(IReadOnlyList<T> items, string nothing, Func<T, Task>? singleItem, Func<IReadOnlyList<T>, string>? manyPrelude, Func<T, int, string> itemToString)
    {
        await DisplayItemList(items, () => nothing, singleItem, manyPrelude, itemToString);
    }
    #endregion

    #region reply
    protected async Task<IUserMessage> TypingReplyAsync(string message, bool isTTS = false, Embed? embed = null, RequestOptions? options = null)
    {
        return await Context.Channel.TypingReplyAsync(message, isTTS, embed, options);
    }

    /// <summary>
    /// Stream out a message from an async source, editing it into the message step by step
    /// </summary>
    /// <param name="message">Async source of message chunks to send (will be joined by spaces)</param>
    /// <param name="messageReference">Optional message reference</param>
    /// <param name="initialPing">Whether the initial message should "ping" the user</param>
    /// <param name="initialMs">How long to wait before sending the initial message</param>
    /// <param name="waitBetweenEdits">How long to wait between edits to the message</param>
    /// <returns></returns>
    protected async Task TypingReplyAsync(IAsyncEnumerable<string> message, MessageReference? messageReference = null, bool initialPing = false, int initialMs = 3250, int waitBetweenEdits = 1250)
    {
        using var typing = Context.Channel.EnterTypingState();

        var initialising = true;
        var contents = new StringBuilder();
        IUserMessage? msg = null;

        var timer = new Stopwatch();
        timer.Start();

        await foreach (var word in message)
        {
            contents.Append(word);

            if (initialising)
            {
                if (timer.ElapsedMilliseconds > initialMs)
                {
                    initialising = false;
                    timer.Restart();
                    await UpdateMessage();
                }
            }
            else
            {
                if (timer.ElapsedMilliseconds > waitBetweenEdits)
                {
                    timer.Restart();
                    await UpdateMessage();
                }
            }
        }

        await UpdateMessage(true);

        async Task UpdateMessage(bool final = false)
        {
            if (contents.Length == 0)
                return;

            var c = contents.ToString();
            if (!final)
                c += "...";

            if (msg == null)
            {
                // If an initialPing is not wanted send the message with no mentions allowed and then edit mentions back on
                msg = await ReplyAsync(c, messageReference: messageReference, allowedMentions: initialPing ? AllowedMentions.All : AllowedMentions.None);
                if (!initialPing)
                {
                    await msg.ModifyAsync(x =>
                    {
                        x.Content = c;
                        x.AllowedMentions = AllowedMentions.All;
                    });
                }
            }
            else
                await msg.ModifyAsync(x => x.Content = c);
        }
    }

    protected async Task<IUserMessage> ReplyAsync(EmbedBuilder embed, RequestOptions? options = null)
    {
        return await ReplyAsync("", false, embed.Build(), options);
    }

    protected Task<IReadOnlyList<IUserMessage>> LongReplyAsync(string message)
    {
        return Context.Channel.SendLongMessageAsync(message);
    }
    #endregion

    protected override void BeforeExecute(CommandInfo command)
    {
        var method = command.Attributes.OfType<BaseExecuteContextAttribute>();
        var module = GetType().GetCustomAttributes(typeof(BaseExecuteContextAttribute), true).Cast<BaseExecuteContextAttribute>();
        _afterExecuteDisposals = method.Concat(module).Select(eca => eca.StartExecute(Context)).ToArray();

        base.BeforeExecute(command);
    }

    protected override void AfterExecute(CommandInfo command)
    {
        if (_afterExecuteDisposals != null)
            foreach (var item in _afterExecuteDisposals)
                item.EndExecute().GetAwaiter().GetResult();

        base.AfterExecute(command);
    }
}