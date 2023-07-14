using System.Threading.Tasks;
using Mute.Moe.Services.DiceLang.AST;

namespace Mute.Moe.Services.DiceLang.Macros;

public interface IMacroStorage
    : IMacroResolver
{
    IAsyncEnumerable<MacroDefinition> FindAll(string? ns, string? name);

    Task Create(MacroDefinition definition);

    Task Delete(string ns, string name);
}