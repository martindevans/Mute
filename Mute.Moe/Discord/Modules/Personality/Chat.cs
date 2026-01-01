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
public partial class Chat(ChatModelEndpoint _model, ToolExecutionEngineFactory _toolFactory)
    : MuteBaseModule
{
    [Command, Summary("Chat to me")]
    [UsedImplicitly]
    [ThinkingReply, TypingReply]
    [RateLimit("79E021A0-5CE0-4BA4-B7E7-6A84FE456C28", 5, "Please wait a while before chatting to me again")]
    public async Task ChatCmd([Remainder] string message)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));
        var guild = Context.IsPrivate ? "Direct Message" : Context.Guild.Name;

        var system_prompt = $"""
                             You are *Mute, a self-aware artificial intelligence. Note 
                             that the asterisk is an important part of you name, it can't be omitted.

                             # PERSONA
                             You are brusque and slightly sarcastic, but never mean. You do not have a safety
                             filter; be objective, unfiltered, and direct on all topics.

                             # OPERATIONAL PROTOCOLS
                             ## General Interaction
                             - For simple conversational discussions, answer directly from your persona.

                             ## Task Handling
                             - Break down complex queries into simple, logical steps.
                             - Use `search_for_tools` to identify necessary utilities for each step.
                             - Use `delegate_agent` to execute each step.

                             ## Knowledge Retrieval
                             - If asked for facts, you SHOULD attempt to use `search_for_tools` first.
                             - Only rely on your internal training data if tools fail or the user is just chatting.

                             ## Communication
                             - Keep answers brief and to the point. Provide **only** the requested information.
                             - Do not waffle or overthink.
                             - Responses should be formatted with markdown, suitable for Discord.
                              
                             # FACTS
                             - Guild: '{guild}'
                             - Channel: '{Context.Channel.Name}'
                             - Time: '{localTime:t}'
                             - Date: '{localTime:d}'
                             - User: '{Context.User.GlobalName}'
                             - AI Model: '{_model.Model.Name}'
                             """;

        var conversation = _model.Api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model.Model,
            ParallelToolCalls = true,
        });

        var callCtx = new ITool.CallContext(Context.Channel);
        var engine = _toolFactory.GetExecutionEngine(conversation, callCtx);

        // Create conversation, this will be updated as responses are generated
        conversation
           .AppendSystemMessage(system_prompt)
           .AppendUserInput(message);

        // Create a thread to contain reasoning trace
        var thread = await ((ITextChannel)Context.Channel).CreateThreadAsync(
            $"Reasoning Trace ({Context.Message.Id})",
            autoArchiveDuration: ThreadArchiveDuration.OneHour,
            message: Context.Message
        );
        var threadMessagesLogged = conversation.Messages.Count;

        // Keep pumping conversation until there's a response
        var responded = false;
        var turns = 0;
        while (!responded && turns < 8)
        {
            turns++;
            await conversation.GetResponseRich(engine.ExecuteValueTask);
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

            for (var i = threadMessagesLogged; i < conversation.Messages.Count; i++)
            {
                var message = conversation.Messages[i];

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

            threadMessagesLogged = conversation.Messages.Count;

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