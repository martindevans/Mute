using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Interactions;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

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
        var channel = Context.Channel;

        // Get the conversation. If it's loading wait a little bit, hopefully we get better stats that way.
        var conversation = await _conversations.GetConversation(channel);
        if (conversation.State == LlmChatConversation.ProcessingState.Loading)
            await Task.Delay(250);

        var embed = new EmbedBuilder()
                   .WithTitle($"Active Conversation for {conversation.Channel.Name}")
                   .WithTimestamp(conversation.LastUpdated)
                   .WithDescription(conversation.Summary ?? "No summary available");

        embed.WithFields(
            new EmbedFieldBuilder().WithIsInline(true).WithName("Event Queue").WithValue(conversation.QueueCount.ToString()),
            new EmbedFieldBuilder().WithIsInline(true).WithName("Message Count").WithValue(conversation.MessageCount.ToString()),
            new EmbedFieldBuilder().WithIsInline(true).WithName("Context Usage").WithValue(conversation.ContextUsage.ToString("P1", CultureInfo.InvariantCulture)),
            new EmbedFieldBuilder().WithIsInline(true).WithName("Processing State").WithValue(conversation.State.ToString())
        );

        // Color ramp based on context usage
        (float r, float g, float b) color = conversation.ContextUsage switch
        {
            < 0.15f => (0.0f, 1.0f, 0.0f),   // green
            < 0.22f => (0.0f, 0.8f, 0.4f),   // green-cyan
            < 0.30f => (0.0f, 0.5f, 1.0f),   // blue
            < 0.38f => (0.4f, 0.7f, 1.0f),   // light blue
            < 0.46f => (1.0f, 1.0f, 0.0f),   // yellow
            < 0.54f => (1.0f, 0.8f, 0.0f),   // yellow-orange
            < 0.62f => (1.0f, 0.5f, 0.0f),   // orange
            < 0.75f => (1.0f, 0.25f, 0.0f),  // deep orange
            _       => (1.0f, 0.0f, 0.0f),   // red
        };
        embed.WithColor(color.r, color.g, color.b);

        // Action buttons
        var buttonRow = new ActionRowBuilder();
        buttonRow.AddComponent(ButtonBuilder.CreateDangerButton("Destroy State", ChatInteractions.InteractionIdClearConversationState));
        buttonRow.AddComponent(ButtonBuilder.CreateSecondaryButton("Force Summarisation", ChatInteractions.InteractionIdSummariseConversationState));
        var components = new ComponentBuilder();
        components.AddRow(buttonRow);

        await ReplyAsync(embed:embed.Build(), components:components.Build());
    }
}

[UsedImplicitly]
[Group("llm")]
public partial class LLM(IToolIndex _tools, MultiEndpointProvider<LLamaServerEndpoint> _backends)
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

    [Command("status"), Summary("List all available LLM backends and their current status")]
    [UsedImplicitly]
    public async Task LlmBackendStatus()
    {
        var results = await _backends.GetStatus();

        var count = 0;
        var desc = new StringBuilder();
        foreach (var result in results)
        {
            desc.AppendLine($"**{result.Endpoint.ID}**");

            if (result.Healthy)
            {
                desc.AppendLine($" - Online 🟢");
                desc.AppendLine($" - Available: {result.AvailableSlots}/{result.MaxSlots}");
                desc.AppendLine($" - Latency: {result.Latency.TotalMilliseconds:##.#}ms");
            }
            else
            {
                desc.AppendLine(" - Offline 🔴");
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
}