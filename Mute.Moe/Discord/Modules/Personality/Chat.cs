using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mute.Moe.Discord.Interaction;

namespace Mute.Moe.Discord.Modules.Personality;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Development playground for LLM stuff
/// </summary>
[UsedImplicitly]
[Group("chat")]
public partial class Chat(ConversationalResponseService _conversations)
    : MuteBaseModule
{
    [Command("state"), Summary("I will show the conversation state for the current channel")]
    [UsedImplicitly]
    public async Task ConversationState()
    {
        // Send message with embed
        var embed = await Context.Channel.GetConversationStateEmbed(_conversations);
        var message = await ReplyAsync(embed: embed.Build());

        // Action buttons
        var buttonRow = new ActionRowBuilder();
        buttonRow.AddComponent(ButtonBuilder.CreateDangerButton("Destroy", ChatInteractions.InteractionIdClearConversationState));
        buttonRow.AddComponent(ButtonBuilder.CreateSecondaryButton("Summarise", ChatInteractions.InteractionIdSummariseConversationState));
        buttonRow.AddComponent(ButtonBuilder.CreateSecondaryButton("Update", ChatInteractions.GetInteractionRefreshId(message)));
        var components = new ComponentBuilder();
        components.AddRow(buttonRow);

        // Edit in action buttons
        await message.ModifyAsync(ctx => ctx.Components = components.Build());
    }
}

[UsedImplicitly]
[Group("llm")]
public partial class LLM(IToolIndex _tools, MultiEndpointProvider<LLamaServerEndpoint> _backends)
    : MuteBaseModule
{
    [Command("tools"), Summary("Search for tools")]
    [UsedImplicitly]
    public async Task ToolSearch([Remainder] string query)
    {
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
            var results = (await _tools.Find(query, topK:5, topP:0)).ToArray();

            // Display results
            await DisplayItemList(
                results,
                () => "No results",
                rs => $"{rs.Count} results",
                (item, idx) => $"{idx + 1}. **{item.Tool.Name}**: {item.Relevance}"
            );
        }
    }

    [Command("status"), Summary("List all available LLM backends and their current status")]
    [UsedImplicitly]
    public async Task ShowBackendStatus()
    {
        var results = await _backends.GetStatus();

        var count = 0;
        var desc = new StringBuilder();
        foreach (var result in results)
        {
            desc.AppendLine($"**{result.Endpoint.ID}**");

            if (result.Healthy)
            {
                desc.AppendLine( " - Online 🟢");
                desc.AppendLine($" - Available: {result.AvailableSlots}/{result.MaxSlots}");
                desc.AppendLine($" - Latency: {result.Latency.TotalMilliseconds:##.#}ms");
            }
            else
            {
                desc.AppendLine(" - Offline 🔴");
                desc.AppendLine($" - Available: 0/{result.MaxSlots}");
            }

            if (result.Healthy)
                count++;
        }

        var color = count switch
        {
            <= 0 => Color.Red,
               1 => Color.Orange,
             > 1 => Color.Green,
        };

        var embed = new EmbedBuilder()
            .WithTitle("LLM Backend Status")
            .WithDescription(desc.ToString())
            .WithColor(color)
            .WithFooter("🧠 Mugunghwa AI Cluster");

        await ReplyAsync(embed: embed.Build());
    }

    [Group("tool")]
    public class Tool(IToolIndex _tools, MultiEndpointProvider<LLamaServerEndpoint> _backends, IToolLog _log)
        : MuteBaseModule
    {
        [Command, Summary("Get all the detailed information for a specific tool")]
        [UsedImplicitly]
        public async Task ShowToolInfo([Remainder] string name)
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

        [Command("log"), Summary("Show tool execution log")]
        [UsedImplicitly]
        [RequireOwner]
        public async Task ShowToolLog(int limit = 8)
        {
            var ctx = Context.AgentMemoryContextId;
            var calls = _log.Calls(ctx).Take(limit).ToArray();
            var responses = calls
                .Select(a => (a, _log.Responses(ctx, id: a.Id).SingleOrDefault()))
                .Where(a => a.Item2 != null)
                .ToDictionary(a => a.a.Id, a => a.Item2!);
            
            await DisplayItemList(
                calls,
                () => "No tool calls have been logged",
                list => $"{list.Count} calls (most recent first):",
                (a, _) => $" - {SingleItem(a)}"
            );

            string SingleItem(IToolLog.ToolCall call)
            {
                // No response! (red blob)
                if (!responses.TryGetValue(call.Id, out var response))
                    return $"🔴 `{call.Name}` **No Response!**";

                var elapsedMs = Math.Round((response.Timestamp - call.Timestamp).TotalMilliseconds);

                // Try to extract "parameters" from call JSON
                var parameters = call.Parameters;
                var jobj = JsonNode.Parse(call.Parameters);
                if (jobj?["arguments"] is JsonNode args)
                    parameters = args.ToJsonString();
                if (parameters == "\"{}\"")
                    parameters = "";
                parameters = Regex.Unescape(parameters);
                parameters = parameters.Replace('`', '\'');

                // Format the response
                var responseStr = response.Value;
                if (responseStr is { Length: > 256 })
                    responseStr = $"{responseStr[..256]}...";
                responseStr = responseStr?.Replace('`', '\'');

                // Failure (orange blob)
                if (!response.Success)
                    return $"🟠 `{call.Name}({parameters}) => {responseStr}` ({elapsedMs}ms)";

                // Success (green blob)
                return $"🟢 `{call.Name}({parameters}) => {responseStr}` ({elapsedMs}ms)";
            }
        }
    }
}