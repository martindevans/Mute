using LlmTornado.Chat;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Services.LLM;
using Serilog;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Code;

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
                         Do not infer or assume facts that are not explicitly provided. When producing an answer, be short and to the point.
                         When beginning a task, call `search_for_tools` to discover any tools that may assist. If at any point you need further information
                         or capabilities, call `search_for_tools` again.
                         If no suitable tool exists, respond with a short, direct description of the exact information or capabilities you lack.
                         Do not take any other actions.
                         """;
    }

    /// <summary>
    /// Create a sub-agent which will attempt to solve a specific task.
    /// </summary>
    /// <param name="callContext"></param>
    /// <param name="task">A clear description of the task to solve</param>
    /// <param name="context">All necessary context required to solve the task</param>
    /// <param name="facts">An array of facts relevant to the task, each fact should be a single sentence description of a clear fact</param>
    /// <param name="tools">An array of tools that will be made available to the sub agent</param>
    /// <returns></returns>
    private async Task<object> DelegateAgent(ITool.CallContext callContext, string task, string context, string[] facts, string[] tools)
    {
        var conversation = _model.Api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model.Model,
            MaxTokens = _model.Model.ContextTokens ?? 4096,
        });

        // Create an execution engine
        var toolFactory = _services.GetRequiredService<ToolExecutionEngineFactory>();
        var engine = toolFactory.GetExecutionEngine(conversation, toolSearch:true, defaultTools:true, context: callContext);

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

        if (!string.IsNullOrWhiteSpace(context))
        {
            prompt.AppendLine("## Context");
            prompt.AppendLine(context);
            prompt.AppendLine();
        }

        if (facts.Length > 0)
        {
            prompt.AppendLine("## Facts");
            foreach (var fact in facts)
                prompt.AppendLine($" - {fact}");
            prompt.AppendLine();
        }

        Log.Information("Spawning LLM Agent. Prompt: {0}", prompt);

        // Create the conversation
        conversation
           .AppendSystemMessage(SubAgentPrompt)
           .AppendUserInput(prompt.ToString());

        // Get response
        var toolCalls = new HashSet<string>();
        for (var i = 0; i < 5; i++)
        {
            await conversation.GetResponseRich(async calls =>
            {
                foreach (var call in calls)
                    toolCalls.Add(call.Name);

                await engine.Execute(calls);
            });

            // Return result as soon as the assistant generates a message
            if (conversation.Messages[^1].Role == ChatMessageRoles.Assistant)
            {
                return new
                {
                    ToolsUsed = toolCalls.ToArray(),
                    Response = conversation.Messages[^1].Content ?? "",
                };
            }
        }

        // We went multiple turns without generating a response
        return new
        {
            ToolsUsed = toolCalls.ToArray(),
            Response = "Failed."
        };
    }
}