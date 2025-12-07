using System.Threading.Tasks;
using Mute.Moe.Services.DiceLang.AST;

namespace Mute.Moe.Services.DiceLang.Macros;

/// <summary>
/// Returns null for all macros
/// </summary>
public class NullMacroResolver
    : IMacroResolver
{
    /// <inheritdoc />
    public async Task<MacroDefinition?> Find(string? ns, string name)
    {
        return null;
    }
}