using Discord.Commands;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.LLM.Memory;
using Mute.Moe.Utilities;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Personality;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[RequireOwner]
public partial class Dev(IKeyValueStorage<AgentDomainDocument> docs)
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
}