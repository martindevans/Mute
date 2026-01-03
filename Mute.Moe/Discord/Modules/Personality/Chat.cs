using System.Text;
using Discord;
using Discord.Commands;
using LlmTornado.Chat;
using LlmTornado.Code;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Personality;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Development playground for LLM stuff
/// </summary>
[UsedImplicitly]
[Group("chat")]
public partial class Chat(ChatConversationFactory _chatFactory)
    : MuteBaseModule
{
    [Command, Summary("Chat to me")]
    [UsedImplicitly]
    [ThinkingReply, TypingReply]
    [RateLimit("79E021A0-5CE0-4BA4-B7E7-6A84FE456C28", 5, "Please wait a while before chatting to me again")]
    public async Task ChatCmd([Remainder] string message)
    {
        // Create conversation
        var conversation = await _chatFactory.Create(Context.Channel);

        // Add initial message
        await conversation.AddUserMessage(Context.User.GlobalName, message);

        // Create a thread to contain reasoning trace
        var thread = await ((ITextChannel)Context.Channel).CreateThreadAsync(
            $"Reasoning Trace ({Context.Message.Id})",
            autoArchiveDuration: ThreadArchiveDuration.OneHour,
            message: Context.Message
        );
        var threadMessagesLogged = conversation.Conversation.Messages.Count;

        // Keep pumping conversation until there's a response
        var responded = false;
        var turns = 0;
        while (!responded && turns < 8)
        {
            turns++;
            await conversation.Conversation.GetResponseRich(conversation.ToolExecutionEngine.ExecuteValueTask);
            responded = await UpdateTrace();
        }

        // Failure notification
        if (!responded)
            await ReplyAsync("No response from model");

        // Reasoning trace is complete, lock thread
        await thread.ModifyAsync(t => t.Locked = true);

        // Steps through conversation, logging out parts to Discord
        async ValueTask<bool> UpdateTrace()
        {
            var responded = false;

            for (var i = threadMessagesLogged; i < conversation.Conversation.Messages.Count; i++)
            {
                var message = conversation.Conversation.Messages[i];

                // Log tool calls
                if (message is { Role: ChatMessageRoles.Tool })
                    await thread.SendLongMessageAsync($"**Tool call result**: `{message.GetMessageContent()}`");

                // Ignore nn-assistant messages
                if (message.Role != null && message.Role != ChatMessageRoles.Assistant)
                    continue;

                // Log reasoning
                var reasoning = message.Reasoning ?? message.ReasoningContent;
                if (reasoning != null)
                    await thread.SendLongMessageAsync(reasoning);

                if (message.ToolCalls != null)
                {
                    foreach (var call in message.ToolCalls)
                    {
                        if (call.FunctionCall != null)
                        {
                            await thread.SendLongMessageAsync(
                                $"**Tool Call: `{call.FunctionCall.Name}`**\n" +
                                $"Args: `{call.FunctionCall.Arguments}`\n"
                            );
                        }
                    }
                }

                // Log actual response
                if (message.Content != null)
                {
                    await LongReplyAsync(message.Content);
                    responded = true;
                }

                // Handle the individual parts
                if (message.Parts != null)
                {
                    foreach (var part in message.Parts)
                    {
                        switch (part.Type)
                        {
                            case ChatMessageTypes.Text:
                                await LongReplyAsync(part.Text ?? "");
                                responded = true;
                                break;

                            case ChatMessageTypes.Reasoning:
                                await thread.SendLongMessageAsync(part.Reasoning?.Content ?? "");
                                break;

                            case ChatMessageTypes.Image:
                            case ChatMessageTypes.Audio:
                            case ChatMessageTypes.FileLink:
                            case ChatMessageTypes.Document:
                            case ChatMessageTypes.SearchResult:
                            case ChatMessageTypes.Video:
                            case ChatMessageTypes.ExecutableCode:
                            case ChatMessageTypes.CodeExecutionResult:
                            case ChatMessageTypes.ContainerUpload:
                            case ChatMessageTypes.Reference:
                                await thread.SendMessageAsync($"Message part type '{part.Type}' not handled yet");
                                break;

                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            threadMessagesLogged = conversation.Conversation.Messages.Count;

            return responded;
        }
    }
}

[UsedImplicitly]
[Group("llm")]
public partial class LLM(IToolIndex _tools, ChatModelEndpoint _chat)
    : MuteBaseModule
{
    [Command("tools"), Summary("Search for tools")]
    [UsedImplicitly]
    public async Task LlmToolSearch(string query, int n = 5)
    {
        // Ensure we can't exceed a sensible number
        n = Math.Min(10, n);

        if (string.IsNullOrEmpty(query) || query == "*")
        {
            // Get all tools, defaults first
            var defaults = _tools.Tools.Where(t => t.Value.IsDefaultTool).Select(t => t.Value);
            var others = _tools.Tools.Where(t => !t.Value.IsDefaultTool).Select(t => t.Value);
            var results = defaults.Concat(others).ToArray();

            // Display results
            await DisplayItemList(
                results,
                () => "No results",
                rs => $"{rs.Count} results",
                (item, idx) => $"{idx + 1}. **{item.Name}**"
            );
        }
        else
        {
            // Do tool search
            var results = (await _tools.Find(query, n)).ToArray();

            // Display results
            await DisplayItemList(
                results,
                () => "No results",
                rs => $"{rs.Count} results",
                (item, idx) => $"{idx + 1}. **{item.Tool.Name}**: {item.Relevance}"
            );
        }
    }

    [Command("tool"), Summary("Get all the detailed information for a specific tool")]
    [UsedImplicitly]
    public async Task LlmToolInfo(string name)
    {
        if (!_tools.Tools.TryGetValue(name, out var tool))
        {
            // Failed to find tool, find similar names
            var nearby = (
                from t in _tools.Tools
                let dist = t.Value.Name.Levenshtein(name)
                orderby dist
                select t.Value
            ).Take(5);

            var builder = new StringBuilder();
            builder.AppendLine($"Cannot find tool `{name}`. Did you mean:");
            foreach (var item in nearby)
                builder.AppendLine($"- {item.Name}");

            await ReplyAsync(builder.ToString());
        }
        else
        {
            var description = new StringBuilder();
            description.AppendLine(tool.Description);
            description.AppendLine();
            description.AppendLine("**Parameters**");

            foreach (var parameter in tool.GetParameters())
            {
                description.AppendLine($" - **{parameter.Name}** (`{parameter.Type.Type}`)");
                description.AppendLine($"  - {parameter.Type.Description}");
                if (!parameter.Type.Required)
                    description.AppendLine("  - Optional");
            }

            var embed = new EmbedBuilder()
                       .WithTitle(tool.Name)
                       .WithDescription(description.ToString())
                       .WithColor(tool.IsDefaultTool ? Color.Gold : Color.LightGrey)
                       .WithFields(
                            new EmbedFieldBuilder().WithIsInline(true).WithName("Default Tool").WithValue(tool.IsDefaultTool)
                        );

            await ReplyAsync(embed: embed.Build());
        }
    }

    [Command("model"), Summary("Get detailed model info")]
    [UsedImplicitly]
    public async Task LlmModelInfo()
    {
        var models = await _chat.Api.Models.GetModels(LLmProviders.Custom);
        if (models == null)
        {
            await ReplyAsync("Cannot fetch model list from backend");
            return;
        }

        var model = models.FirstOrDefault(a => string.Equals(a.Id, _chat.Model.Name, StringComparison.OrdinalIgnoreCase) || string.Equals(a.Name, _chat.Model.Name, StringComparison.OrdinalIgnoreCase));
        if (model == null)
        {
            await ReplyAsync("Cannot find chat model in backend");
            return;
        }

        var embed = new EmbedBuilder()
                   .WithTitle(model.Name ?? model.Id);

        if (model.Description != null)
            embed.WithDescription(model.Description ?? "");

        var fields = new List<EmbedFieldBuilder>();

        if (model.ContextLength != null)
            fields.Add(new EmbedFieldBuilder().WithIsInline(true).WithName("Context").WithValue(model.ContextLength));

        embed.WithFields(fields);

        await ReplyAsync(embed: embed);
    }
}