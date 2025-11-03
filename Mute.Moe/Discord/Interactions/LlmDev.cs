using Discord.Commands;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Modules;
using Mute.Moe.Services.Information.Weather;
using Mute.Moe.Services.LLM;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Services.Information.Geocoding;
using Mute.Moe.Tools;

namespace Mute.Moe.Discord.Interactions;

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
    private readonly ILlmPermission _permission;
    private readonly IWeather _weather;
    private readonly IGeocoding _geocoding;
    private readonly ToolExecutionEngineFactory _toolFactory;
    private readonly IToolIndex _tools;

    public LlmDev(TornadoApi api, ChatModel model, ILlmPermission permission, IWeather weather, IGeocoding geocoding, ToolExecutionEngineFactory toolFactory, IToolIndex tools)
    {
        _api = api;
        _model = model;
        _permission = permission;
        _weather = weather;
        _geocoding = geocoding;
        _toolFactory = toolFactory;
        _tools = tools;
    }

    [Command("llm-perm"), RequireOwner, Summary("I will remember that you have given permission for remote LLM usage")]
    [UsedImplicitly]
    public async Task LlmPerm(bool value = true)
    {
        await _permission.SetPermission(Context.User.Id, value);

        if (value)
            await ReplyAsync("Ok, I'll remember that you've allowed use of remotely hosted LLMs. You can use this command again with a 'false' parameter to withdraw permission");
        else
            await ReplyAsync("Ok, I won't use remote LLMs to process any of your message content");
    }

    [Command("llm-tool-search"), RequireOwner, Summary("Fuzzy search for tools")]
    [UsedImplicitly]
    public async Task LlmToolSearch(string query, int n = 5)
    {
        var results = (await _tools.Find(query)).Take(n).ToArray();

        await DisplayItemList(results, () => ReplyAsync("No results"), rs => ReplyAsync($"{rs.Count} results"), (item, idx) => ReplyAsync($"{idx + 1}. {item.Tool.Name}: {item.Tool.Description} ({item.Similarity})"));
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

        var replies = await LongReplyAsync(message.ToString());

        if (reasoning.Length > 0)
        {
            var thread = await ((ITextChannel)Context.Channel).CreateThreadAsync("Reasoning", autoArchiveDuration: ThreadArchiveDuration.OneHour, message: replies[^1]);
            await thread.SendLongMessageAsync(reasoning.ToString());
            await thread.ModifyAsync(t => t.Locked = true);
        }
    }
}