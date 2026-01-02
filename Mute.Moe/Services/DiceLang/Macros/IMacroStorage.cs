using System.Threading.Tasks;
using Mute.Moe.Services.DiceLang.AST;

namespace Mute.Moe.Services.DiceLang.Macros;

/// <summary>
/// Store macros for dicelang
/// </summary>
public interface IMacroStorage
    : IMacroResolver
{
    /// <summary>
    /// Find all macros which match the query.
    /// </summary>
    /// <param name="ns">Optional namespace</param>
    /// <param name="name">Optional name</param>
    /// <returns></returns>
    IAsyncEnumerable<MacroDefinition> FindAll(string? ns, string? name);

    /// <summary>
    /// Create a new macro
    /// </summary>
    /// <param name="definition"></param>
    /// <returns></returns>
    Task Create(MacroDefinition definition);

    /// <summary>
    /// Delete a macro
    /// </summary>
    /// <param name="ns"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<bool> Delete(string ns, string name);
}