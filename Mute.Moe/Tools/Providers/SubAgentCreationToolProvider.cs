using LlmTornado.Chat;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Services.LLM;
using Serilog;
using System.Text;
using System.Threading.Tasks;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides tools for spawning sub agent
/// </summary>
public class SubAgentCreationToolProvider
    : IToolProvider
{
    private readonly ChatModelEndpoint _model;
    private readonly IServiceProvider _services;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Main system prompt for sub agents
    /// </summary>
    public string SubAgentPrompt { get; }

    /// <summary>
    /// Create <see cref="SubAgentCreationToolProvider"/>
    /// </summary>
    /// <param name="chat">LLM and API to use</param>
    /// <param name="services"></param>
    public SubAgentCreationToolProvider(ChatModelEndpoint chat, IServiceProvider services)
    {
        _model = chat;
        _services = services;

        Tools =
        [
            new AutoTool("delegate_agent", true, DelegateAgent),
        ];

        SubAgentPrompt = """
                         You are a single-purpose worker agent. Complete the assigned task using only the information provided or obtained through approved tools.
                         Do not infer or assume facts that are not explicitly supplied. When producing an answer, be short and to the point.
                         When beginning a task, call `search_for_tools` to discover any tools that may assist. At any point, if you need further information
                         or capabilities, call `search_for_tools` again.
                         If no suitable tool exists, respond with a short, direct description of the exact information or capabilities you lack.
                         Do not take any other actions.
                         """;
    }

    /// <summary>
    /// Create a sub-agent which will attempt to solve a specific task.
    /// </summary>
    /// <param name="task">A clear description of the task to solve</param>
    /// <param name="context">All necessary context required to solve the task</param>
    /// <param name="facts">An array of facts relevant to the task, each fact should be a single sentence description of s clear fact</param>
    /// <param name="tools">An array of tools that will be made available to the sub agent</param>
    /// <returns></returns>
    private async Task<object> DelegateAgent(string task, string context, string[] facts, string[] tools)
    {
        var conversation = _model.Api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model.Model,
            MaxTokens = 5_000,
        });

        // Create an execution engine
        var toolFactory = _services.GetRequiredService<ToolExecutionEngineFactory>();
        var engine = toolFactory.GetExecutionEngine(conversation, toolSearch:true, defaultTools:true);

        // Do not allow agents to delegate to new agents
        await engine.BanTool("delegate_agent");

        // Add all of the initial tools
        foreach (var tool in tools)
            await engine.AddTool(tool);

        // Build the prompt
        var prompt = new StringBuilder();
        prompt.AppendLine("# Task");
        prompt.AppendLine(task);
        prompt.AppendLine();
        prompt.AppendLine("## Context");
        prompt.AppendLine(context);
        prompt.AppendLine();
        prompt.AppendLine("## Facts");
        foreach (var fact in facts)
            prompt.AppendLine($" - {fact}");

        Log.Information("Spawning LLM Agent. Prompt: {0}", prompt);

        // Prompt the conversation
        conversation
           .AppendSystemMessage(SubAgentPrompt)
           .AppendUserInput(prompt.ToString());

        // Run the conversation
        var message = new StringBuilder();
        var reasoning = new StringBuilder();
        var callCount = 0;
        var handler = new ChatStreamEventHandler
        {
            MessageTokenHandler = x =>
            {
                message.Append(x);
                return ValueTask.CompletedTask;
            },
            ReasoningTokenHandler = x =>
            {
                reasoning.Append(x.Content ?? "");
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = async calls =>
            {
                callCount++;
                await engine.Execute(calls);
            },
            AfterFunctionCallsResolvedHandler = async (_, handler) =>
            {
                await conversation.StreamResponseRich(handler);
            }
        };
        await conversation.StreamResponseRich(handler);

        // Return the result
        return new
        {
            ReasoningTokenCount = reasoning.Length,
            ToolsUsed = engine.AvailableTools.Keys.ToArray(),
            ToolCallsCount = callCount,
            Response = message.ToString()
        };
    }
}