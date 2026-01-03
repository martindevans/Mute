using System.Globalization;
using System.Text;
using Discord;
using Discord.Commands;
using LlmTornado.Code;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using System.Threading.Tasks;
using Mute.Moe.Discord.Services.Responses;

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
        var conversation = await _conversations.GetConversation(channel);

        if (conversation == null)
        {
            await ReplyAsync("No active conversation");
        }
        else
        {
            var embed = new EmbedBuilder()
                       .WithTitle($"Active Conversation for {conversation.Channel.Name}")
                       .WithTimestamp(conversation.LastUpdated)
                       .WithDescription(conversation.Summary ?? "No summary available");

            embed.WithFields(
                new EmbedFieldBuilder().WithIsInline(true).WithName("Queue Depth").WithValue(conversation.QueueCount.ToString()),
                new EmbedFieldBuilder().WithIsInline(true).WithName("Message Count").WithValue(conversation.MessageCount.ToString()),
                new EmbedFieldBuilder().WithIsInline(true).WithName("Context Usage").WithValue(conversation.ContextUsage.ToString("P1", CultureInfo.InvariantCulture)),
                new EmbedFieldBuilder().WithIsInline(true).WithName("Processing State").WithValue(conversation.State.ToString())
            );

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

            await ReplyAsync(embed);
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