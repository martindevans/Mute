using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat;

namespace Mute.Moe.Services.LLM.Memory.Extraction;

/// <summary>
/// Extracts facts from conversation transcript
/// </summary>
public class FactExtractionService
{
    private readonly LlmFactModel _model;
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;

    private readonly AgentFactExtractionSystemPrompt _extractionPrompt;

    /// <summary>
    /// Create a new fact extractor
    /// </summary>
    /// <param name="model"></param>
    /// <param name="endpoints"></param>
    /// <param name="extractionPrompt"></param>
    public FactExtractionService(
        LlmFactModel model,
        MultiEndpointProvider<LLamaServerEndpoint> endpoints,
        AgentFactExtractionSystemPrompt extractionPrompt
    )
    {
        _model = model;
        _endpoints = endpoints;
        _extractionPrompt = extractionPrompt;
    }

    /// <summary>
    /// Extract facts from the given conversation transcript
    /// </summary>
    /// <param name="transcript"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<MemoryItem>> Extract(string transcript, CancellationToken ct = default)
    {
        var result = new List<MemoryItem>();

        // Setup chat request
        var request = new ChatRequest
        {
            Model = _model.Model,
        };

        // Sampling parameters
        _model.Sampling?.Apply(request);

        // Clone conversation
        var conv = new ChatConversation(request, _model, null, _endpoints);

        // Replace with initial fact extraction prompt
        conv.ReplaceSystemPrompt(_extractionPrompt.Prompt);

        // Add the original conversation as a transcript
        conv.AddAnonymousUserMessage(transcript);

        // Generate a response to extract facts
        var response1 = await conv.GenerateResponseMultiStep(2, ct: ct) ?? "";

        // Parse items
        foreach (var item in response1.EnumerateLines())
        {
            var line = item
                      .Trim(' ')
                      .TrimStartCaseInsensitive("- we learned that")
                      .Trim()
                      .TrimEnd('.');

            result.Add(new MemoryItem(new string(line)));
        }

        /* Followup

        // Create new conversation
        conv.Clear(true);

        // Generate link facts
        conv.ReplaceSystemPrompt(
            _linkingPrompt.Prompt
                          .Replace("{subjects}", string.Join("\n", subjectsSet.Select(a => $" - {a}")))
                          .Replace("{facts}", string.Join("\n", facts.Select(a => $" - {a}")))
        );

        // Add the original conversation as a transcript
        conv.AddAnonymousUserMessage(transcript);

        // Generate a response to extract facts
        var response2 = await conv.GenerateResponseMultiStep(2, ct: ct) ?? "";

        // Extract new link facts
        var linkFacts = new List<MemoryItem>();
        MemoryItem.ReadMany(response2, linkFacts, invalidFacts);

        */

        return result;
    }
}