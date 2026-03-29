using Discord.Commands;
using LlmTornado.Agents;
using LlmTornado.Mcp;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.LLM;
using Mute.Moe.Services.LLM.Memory;
using Mute.Moe.Utilities;
using Serilog;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Personality;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[RequireOwner]
public partial class Dev(LlmChatModel model, IKeyValueStorage<AgentDomainDocument> docs, MultiEndpointProvider<LLamaServerEndpoint> server)
    : MuteBaseModule
{
    [Command("create_domain_doc")]
    [UsedImplicitly]
    public async Task CreateDomainDoc()
    {
        var str = "";
        var edits = new List<StringEdit>
        {
            new(StringEditType.Insert, 0, "Hello", 1),
            new(StringEditType.Insert, 5, "World", 1),
            new(StringEditType.Insert, 5, " ", 1),
        };
        str = edits[0].Apply(str);
        str = edits[1].Apply(str);
        str = edits[2].Apply(str);

        var doc = new AgentDomainDocument
        {
            Sections =
            [
                new AgentDomainDocumentSection
                {
                    ID = 123,
                    Content = str,
                    Summary = "Hello world, apparently",
                    Edits = edits,
                    Subsections =
                    [
                        new AgentDomainDocumentSection
                        {
                            ID = 34634,
                            Content = "blah",
                            Summary = "blah x 1",
                            Title = "Sub A",
                            Edits = [],
                            Subsections = []
                        },
                        new AgentDomainDocumentSection
                        {
                            ID = 45725,
                            Content = "blah blah",
                            Summary = "blah x 2",
                            Title = "Sub B",
                            Edits = [],
                            Subsections = [],
                            IsClosable = true,
                        }
                    ],
                    AgentCreated = false,
                    AgentEditable = false,
                    IsClosable = false,
                    Title = "Greeting"
                }
            ]
        };

        await docs.Put(0, doc);
    }

    [Command("get_domain_doc")]
    [UsedImplicitly]
    public async Task GetDomainDoc()
    {
        var doc = (await docs.Get(0))!;
        
        await ReplyAsync($"```\n{doc}```");
    }

    [Command("test-mcp2")]
    [UsedImplicitly]
    public async Task Mcp2()
    {
        //var conf = new McpToolProviderConfig("Test", "http://localhost:3001");

        //var provider = await conf.TryCreateProvider();
        //if (provider == null)
        //{
        //    await ReplyAsync("no provider");
        //    return;
        //}

        //var tools = provider.Tools;

        //foreach (var tool in tools)
        //{
        //    await ReplyAsync(tool.Name);
        //    await ReplyAsync(tool.Description);
        //}

        var mcp = new MCPServer(
            serverLabel: "Echo",
            serverUrl: "http://localhost:3001",
            allowedTools: null,
            additionalConnectionHeaders: null
        );

        await mcp.InitializeAsync();

        var client = mcp.McpClient;
        if (client == null)
        {
            Log.Error("Failed to connect to MCP Server: '{0}'@{1}. Tools from this server will be unavailable!", mcp.ServerLabel, mcp.ServerUrl);
            return;
        }

        var tools = await client.ListTornadoToolsAsync();

        using var endpoint = await server.GetEndpoint([ model.Model.Name ], default);
        if (endpoint == null)
            return;

        // Create a basic agent
        var agent = new TornadoAgent(
            client: endpoint.Endpoint.TornadoApi,
            model: model.Model.Name,
            instructions: "You are a helpful assistant."
        );
        agent.AddTool(tools);

        // Run the agent with a simple query
        var result1 = await agent.Run("What is 2+2?");

        // Get the response
        await ReplyAsync(result1.Messages[^1].Content);

        var result2 = await agent.Run("List all available tools.");

        // Get the response
        await ReplyAsync(result2.Messages[^1].Content);

        var result3 = await agent.Run("Use the `echo` tool and print the reply");

        // Get the response
        await ReplyAsync(result3.Messages[^1].Content);

    }
}