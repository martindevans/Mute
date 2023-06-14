using System.Threading.Tasks;
using Mute.Moe.Services.DiceLang.AST;

namespace Mute.Moe.Services.DiceLang.Macros;

public class NullMacroResolver
    : IMacroResolver
{
    public async Task<MacroDefinition?> Find(string? ns, string name)
    {
        return null;

        //// Test definition which adds 2 parameters
        //return new MacroDefinition(
        //    new[] { "x", "y" },
        //    new Add(new Parameter("x"), new Parameter("y"))
        //);
    }
}