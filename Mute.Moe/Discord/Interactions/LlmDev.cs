using Discord.Commands;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Modules;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Tools;

namespace Mute.Moe.Discord.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Development playground for LLM stuff
/// </summary>
[RequireOwner]
[UsedImplicitly]
public class LlmDev
    : BaseModule
{
    private readonly TornadoApi _api;
    private readonly ChatModel _model;
    private readonly ToolExecutionEngineFactory _toolFactory;
    private readonly IToolIndex _tools;

    public LlmDev(TornadoApi api, ChatModel model, ToolExecutionEngineFactory toolFactory, IToolIndex tools)
    {
        _api = api;
        _model = model;
        _toolFactory = toolFactory;
        _tools = tools;
    }

    [Command("llm-tool-search"), RequireOwner, Summary("Fuzzy search for tools")]
    [UsedImplicitly]
    public async Task LlmToolSearch(string query, int n = 5)
    {
        var results = (await _tools.Find(query, 5)).ToArray();

        await DisplayItemList(
            results,
            () => "No results",
            rs => $"{rs.Count} results",
            (item, idx) => $"{idx + 1}. {item.Tool.Name}: {item.Tool.Description} ({item.Similarity})"
        );
    }

    [Command("llm-test"), RequireOwner, Summary("I will respond with an LLM")]
    [UsedImplicitly]
    [ThinkingReply]
    [TypingReply]
    public async Task LlmTest(string sys, string msg)
    {
        var conversation = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model,
            MaxTokens = 10_000,
        });

        var engine = _toolFactory.GetExecutionEngine(conversation);

        conversation
           .AppendSystemMessage(sys)
           .AppendUserInput(msg);

        var message = new StringBuilder();
        var reasoning = new StringBuilder();

        var handler = new ChatStreamEventHandler
        {
            MessageTokenHandler = x =>
            {
                message.Append(x);
                return ValueTask.CompletedTask;
            },
            ReasoningTokenHandler = x =>
            {
                reasoning.Append(x.Content ?? "");
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = async calls =>
            {
                await engine.Execute(calls);
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) =>
            {
                await conversation.StreamResponseRich(handler);
            }
        };

        await conversation.StreamResponseRich(handler);

        if (message.Length == 0)
            message.AppendLine("No Response From LLM");
        var replies = await LongReplyAsync(message.ToString());

        if (reasoning.Length > 0)
        {
            var thread = await ((ITextChannel)Context.Channel).CreateThreadAsync("Reasoning", autoArchiveDuration: ThreadArchiveDuration.OneHour, message: replies[^1]);
            await thread.SendLongMessageAsync(reasoning.ToString());
            await thread.ModifyAsync(t => t.Locked = true);
        }
    }
}