using Discord;
using Discord.Commands;
using HandyAgentFramework.FunctionCall.Middleware.ToolSearch;
using MultiBackendServiceProvider;
using Mute.Moe.Discord.Interaction;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.LLM;
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
        // Send message with embed
        var embed = await Context.Channel.GetConversationStateEmbed(_conversations);
        var message = await ReplyAsync(embed: embed.Build());

        // Action buttons
        var buttonRow = new ActionRowBuilder();
        buttonRow.AddComponent(ButtonBuilder.CreateDangerButton("Destroy", ChatInteractions.InteractionIdClearConversationState));
        buttonRow.AddComponent(ButtonBuilder.CreateSecondaryButton("Refresh", ChatInteractions.GetInteractionRefreshId(message)));
        var components = new ComponentBuilder();
        components.AddRow(buttonRow);

        // Edit in action buttons
        await message.ModifyAsync(ctx => ctx.Components = components.Build());
    }
}

[UsedImplicitly]
[Group("llm")]
public partial class LLM(IToolSet _tools, MultiBackendServiceProvider<LLamaServerEndpoint> _backends)
    : MuteBaseModule
{
    [Command("status"), Summary("List all available LLM backends and their current status")]
    [UsedImplicitly]
    public async Task ShowBackendStatus()
    {
        var results = await _backends.GetStatus(default);

        var count = 0;
        var desc = new StringBuilder();
        foreach (var result in results)
        {
            desc.AppendLine($"**{result.Backend.ID}**");

            if (result.IsHealthy)
            {
                count++;

                desc.AppendLine( " - Online 🟢");
                desc.AppendLine($" - Available: {result.AvailableSlots}/{result.TotalSlots}");
                desc.AppendLine($" - Latency: {result.Latency.TotalMilliseconds:##.#}ms");
            }
            else
            {
                desc.AppendLine(" - Offline 🔴");
                desc.AppendLine($" - Available: 0/{result.TotalSlots}");
            }
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

    [Command("search"), Summary("Search for tools"), Alias("find")]
    [UsedImplicitly]
    public async Task ToolSearch([Remainder] string query)
    {
        if (string.IsNullOrEmpty(query) || query == "*")
        {
            // Get all tools, defaults first
            var defaults = _tools.Tools().Where(t => t.IsDefault);
            var others = _tools.Tools().Where(t => !t.IsDefault);
            var results = defaults.Concat(others).ToArray();

            // Display results
            await DisplayItemList(
                results,
                () => "No results",
                rs => $"{rs.Count} results",
                (item, idx) => $"{idx + 1}. **{item.Function.Name}**"
            );
        }
        else
        {
            // Do tool search
            var results = (await _tools.Search(query, topK: 5)).ToArray();

            // Display results
            await DisplayItemList(
                results,
                () => "No results",
                rs => $"{rs.Count} results",
                (item, idx) => $"{idx + 1}. **{item.Name}**: {item.Relevance}"
            );
        }
    }

    //[Group("tool")]
    //public class Tool(IToolSet _tools)
    //    : MuteBaseModule
    //{
    //    [Command, Summary("Get all the detailed information for a specific tool")]
    //    [UsedImplicitly]
    //    public async Task ShowToolInfo([Remainder] string name)
    //    {
    //        var tool = _tools.TryGetTool(name);

    //        if (tool == null)
    //        {
    //            // Failed to find tool, find similar names
    //            var nearby = (
    //                from t in _tools.Tools()
    //                let dist = t.Function.Name.Levenshtein(name)
    //                orderby dist
    //                select t
    //            ).Take(5);

    //            var builder = new StringBuilder();
    //            builder.AppendLine($"Cannot find tool `{name}`. Did you mean:");
    //            foreach (var item in nearby)
    //                builder.AppendLine($"- {item.Function.Name}");

    //            await ReplyAsync(builder.ToString());
    //        }
    //        else
    //        {
    //            var description = new StringBuilder();
    //            description.AppendLine(tool.Function.Description);
    //            description.AppendLine();
    //            description.AppendLine("**Parameters**");

    //            throw new NotImplementedException("Parse arguments from schema");
                
    //            //foreach (var parameter in tool.GetParameters())
    //            //{
    //            //    description.AppendLine($" - **{parameter.Name}** (`{parameter.Type.Type}`)");
    //            //    description.AppendLine($"  - {parameter.Type.Description}");
    //            //    if (!parameter.Type.Required)
    //            //        description.AppendLine("  - Optional");
    //            //}

    //            //var embed = new EmbedBuilder()
    //            //           .WithTitle(tool.Function.Name)
    //            //           .WithDescription(description.ToString())
    //            //           .WithColor(tool.IsDefault ? Color.Gold : Color.LightGrey)
    //            //           .WithFields(
    //            //                new EmbedFieldBuilder().WithIsInline(true).WithName("Default Tool").WithValue(tool.IsDefault)
    //            //            );

    //            //await ReplyAsync(embed: embed.Build());
    //        }
    //    }
    //}
}