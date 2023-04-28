using System.Collections.Generic;
using System.Linq;
using Discord;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Mute.Moe.Services.LLM;

internal class OpenAIChatCompletion
    : ILargeLanguageModel
{
    private readonly IOpenAIService _api;
    private readonly LargeLanguageModelGenerationOptions _defaults;
    private readonly string _system;
    private readonly string _model = Models.ChatGpt3_5Turbo;

    private const float _frequencyPenalty = 0.05f;
    private const float _presencePenalty = 0.05f;

    public OpenAIChatCompletion(IOpenAIService api, LargeLanguageModelGenerationOptions defaults)
    {
        _api = api;
        _defaults = defaults;

        _system = """
            You an artificial intelligence named *Mute. *Mute is female, helpful, shy and a little abrupt.

            Your task is to add calls to a Question Answering API to a piece of text. The questions should help you get information about the text. You can call the
            API by writing "[QA(question)]" where "question" is the question you want to ask. Here is an example of an API call:

            Input: Where was Joe Biden born?
            Output: Joe Biden was born in [QA("Where was Joe Biden born?")].
            """;
    }

    public EmbedBuilder Summary(EmbedBuilder embed)
    {
        return embed
              .WithTitle("OpenAI")
              .AddField("Model", _model);
    }

    public async IAsyncEnumerable<string> Generate(string prompt, LargeLanguageModelGenerationOptions? options)
    {
        options = _defaults.Merge(options);

        var completionResult = _api.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                new(StaticValues.ChatMessageRoles.System, _system),
                new(StaticValues.ChatMessageRoles.User, prompt),
            },
            Model = _model,
            MaxTokens = options.MaxTokens,
            N = 1,
            FrequencyPenalty = _frequencyPenalty,
            PresencePenalty = _presencePenalty,
            Temperature = options.Temperature,
            TopP = options.TopP,
        });

        await foreach (var completion in completionResult)
        {
            if (!completion.Successful)
                continue;

            var word = completion.Choices.FirstOrDefault()?.Delta.Content;
            if (word != null)
                yield return word;
        }
    }
}