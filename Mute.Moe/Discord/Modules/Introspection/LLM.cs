using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Services.LLM;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
[RequireOwner]
[Group("llm")]
public class LLM
    : BaseModule
{
    private readonly ILargeLanguageModel _llm;

    public LLM(ILargeLanguageModel llm)
    {
        _llm = llm;
    }

    [Command("model"), Summary("I will tell you about my LLM model")]
    public async Task Model()
    {
        var detail = _llm.Summary(new EmbedBuilder());
        await ReplyAsync(detail);
    }

    [Command("prompt"), Summary("I will generate a response using an LLM")]
    public async Task Prompt([Remainder] string prompt)
    {
        if (prompt.StartsWith("\"") && prompt.EndsWith("\""))
            prompt = prompt[1..^1];

        await TypingReplyAsync(
            _llm.Generate(prompt),
            new MessageReference(Context.Message.Id)
        );
    }
}