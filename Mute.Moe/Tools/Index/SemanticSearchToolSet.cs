using HandyAgentFramework;
using HandyAgentFramework.Embedding;
using HandyAgentFramework.Embedding.SqliteCache;
using HandyAgentFramework.FunctionCall.Middleware.ToolSearch;
using Mute.Moe.Services.LLM.Rerank;
using System.Numerics.Tensors;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mute.Moe.Tools.Index;

/// <summary>
/// Represents a toolset that uses semantic search capabilities to find tools.
/// </summary>
public partial class SemanticSearchToolSet
    : IToolSet
{
    private const float TopP = 0.5f;

    private readonly ILogger<SemanticSearchToolSet> _logger;

    private readonly IEmbeddings _embeddings;
    private readonly IReranking _reranking;

    private readonly Task<State> _state;
    
    private readonly ToolDefinition[] _defaultTools;
    private readonly Dictionary<string, ToolDefinition> _allTools;
    private readonly Dictionary<string, ToolDefinition[]> _groups;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchToolSet"/> class.
    /// </summary>
    /// <param name="embeddings">The embeddings provider used for semantic search operations.</param>
    /// <param name="reranking"></param>
    /// <param name="db">The SQLite embedding cache connection provider used to manage cached embeddings.</param>
    /// <param name="tools"></param>
    /// <param name="logger"></param>
    public SemanticSearchToolSet(IEmbeddings embeddings, IReranking reranking, ISqliteEmbeddingCacheConnectionProvider db, IToolProvider[] tools, ILogger<SemanticSearchToolSet> logger)
    {
        _embeddings = embeddings;
        _reranking = reranking;
        _logger = logger;

        _defaultTools = tools.SelectMany(a => a.Tools).Where(a => a.IsDefault).ToArray();
        _allTools = tools.SelectMany(a => a.Tools).ToDictionary(a => a.Function.Name.ToLowerInvariant(), a => a);
        _groups = tools.SelectMany(a => a.Tools).GroupBy(a => a.Group).ToDictionary(a => a.Key, a => a.ToArray());

        _state = Task.Run(() => Init(new SqliteEmbeddingCache(embeddings, db), _allTools.Values.ToArray()));
    }
    
    private static async Task<State> Init(IEmbeddings embedder, IReadOnlyList<ToolDefinition> tools)
    {
        var descriptions = tools.Select(a => a.Function.Description).ToArray();
        var embeddings = await embedder.Embed(descriptions);

        var dict = new Dictionary<ToolDefinition, ReadOnlyMemory<float>>();
        for (var i = 0; i < tools.Count; i++)
            dict[tools[i]] = embeddings[i].Result;

        return new State(dict);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IToolSet.SearchResult>> Search(string query, int? topK = null, CancellationToken cancellation = default)
    {
        LogToolSearchQuery(query);

        // Embed the query
        var queryEmbed = await _embeddings.Embed(query, cancellation);
        
        // Wait for init to finish and retrieve the result
        State state;
        try
        {
            state = await _state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception initialising SemanticSearchToolSet");
            throw;
        }

        // Get tools with dot product to query embedding
        var results = (
            from item in _allTools
            let name = item.Key
            let tool = item.Value
            where tool != null
            let toolEmbedding = state.GetToolEmbedding(tool)
            where toolEmbedding is { IsEmpty: false }
            let dot = TensorPrimitives.CosineSimilarity(queryEmbed.Result.Span, toolEmbedding.Value.Span)
            where !float.IsNaN(dot) && !float.IsInfinity(dot)
            orderby dot descending
            select new IToolSet.SearchResult(name, dot, tool)
        ).Take(topK ?? int.MaxValue).ToArray();

        // Early exit if there's no real re-ranker, just to skip the useless work.
        // Does not apply TopP, since raw embedding dot product is not meaningful enough for that.
        if (_reranking is NullRerank)
            return results;

        var rerankPrompt = $"""
                            ## Task
                            Score how appropriate each tool is for accomplishing the goal.
                            
                            ## Goal
                            {query}
                            
                            ## Instructions
                             - Score high tools that help accomplish the goal
                             - Score low tools that are irrelevant
                             - Consider the functionality of the tool
                             - Prefer tools that directly enable the action
                            """;

        // Rerank the tools based on the query and their description
        var reranking = await _reranking.Rerank(rerankPrompt, results.Select(a => a.Tool.Function.Description).ToArray(), cancellation);

        // New list of results
        var rerankedResults = new List<IToolSet.SearchResult>();
        foreach (var rank in reranking)
            rerankedResults.Add(results[rank.Index] with { Relevance = rank.Relevance });

        return ApplyTopP(rerankedResults, TopP).ToList();
    }

    private static IEnumerable<IToolSet.SearchResult> ApplyTopP(IEnumerable<IToolSet.SearchResult> values, float topP)
    {
        float? threshold = default;
        foreach (var item in values)
        {
            if (!threshold.HasValue)
            {
                threshold = item.Relevance * topP;
            }
            else
            {
                if (item.Relevance < threshold)
                    yield break;
            }

            yield return item;
        }
    }

    /// <inheritdoc />
    public ToolDefinition? TryGetTool(string name)
    {
        return _allTools.GetValueOrDefault(name.ToLowerInvariant());
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> DefaultTools()
    {
        return _defaultTools;
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> Tools()
    {
        return _allTools.Values;
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> GetToolGroup(string group)
    {
        return _groups.GetValueOrDefault(group, [ ]);
    }

    private class State
    {
        private IReadOnlyDictionary<ToolDefinition, ReadOnlyMemory<float>> _toolEmbeddings { get; set; }

        public State(IReadOnlyDictionary<ToolDefinition, ReadOnlyMemory<float>> toolEmbeddings)
        {
            _toolEmbeddings = toolEmbeddings;
        }

        public ReadOnlyMemory<float>? GetToolEmbedding(ToolDefinition tool)
        {
            return _toolEmbeddings.GetValueOrDefault(tool);
        }
    }

    [LoggerMessage(LogLevel.Information, "Tool search: '{query}'")]
    private partial void LogToolSearchQuery(string query);
}