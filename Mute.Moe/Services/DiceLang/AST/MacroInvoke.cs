using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// AST node for invoking a macro/function
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Name"></param>
/// <param name="Arguments"></param>
public record MacroInvoke(string? Namespace, string Name, IReadOnlyList<IAstNode> Arguments)
    : IAstNode
{
    /// <inheritdoc />
    public async Task<double> Evaluate(IAstNode.Context context)
    {
        // Get the macro
        var macro = await context.MacroResolver.Find(Namespace, Name) 
                 ?? throw new MacroNotFoundException(Namespace, Name);

        // Sanity check arg count
        if (macro.ParameterNames.Count != Arguments.Count)
            throw new MacroIncorrectArgumentCount(Namespace, Name, macro.ParameterNames.Count, Arguments.Count);

        // Overwrite any named parameters from the outer context
        var parameters = new Dictionary<string, IAstNode>(context.NamedArgs);
        foreach (var (name, node) in macro.ParameterNames.Zip(Arguments))
            parameters[name] = node;

        // Evaluate the macro AST with the bound parameters
        var ctx = context with { NamedArgs = parameters };
        return await macro.Root.Evaluate(ctx);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var name = string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";
        return $"{name}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
    }
}

/// <summary>
/// Service to find macros
/// </summary>
public interface IMacroResolver
{
    /// <summary>
    /// Resolve a macro
    /// </summary>
    /// <param name="ns">Optional macro namespace</param>
    /// <param name="name">Macro name</param>
    /// <returns></returns>
    Task<MacroDefinition?> Find(string? ns, string name);
}

/// <summary>
/// A macro
/// </summary>
/// <param name="Namespace"></param>
/// <param name="Name"></param>
/// <param name="ParameterNames"></param>
/// <param name="Root"></param>
public record MacroDefinition(string Namespace, string Name, IReadOnlyList<string> ParameterNames, IAstNode Root)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"`{Namespace}::{Name}({string.Join(", ", ParameterNames)}) = {Root}`";
    }
}


/// <summary>
/// Thrown when a macro cannot be found
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
public class MacroNotFoundException(string? ns, string name)
    : Exception($"Failed to find macro `{ns}::{name}`")
{
    /// <summary>
    /// Macro namespace
    /// </summary>
    public string? Namespace { get; } = ns;

    /// <summary>
    /// Macro name
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Thrown when the wrong number of args are passed to a macro
/// </summary>
/// <param name="ns"></param>
/// <param name="name"></param>
/// <param name="expected"></param>
/// <param name="actual"></param>
public class MacroIncorrectArgumentCount(string? ns, string name, int expected, int actual)
    : Exception($"Expected {expected} arguments for macro `{ns}::{name}`, but given {actual}")
{
    /// <summary>
    /// Macro namespace
    /// </summary>
    public string? Namespace { get; } = ns;

    /// <summary>
    /// Macro name
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Expected number of parameters
    /// </summary>
    public int Expected { get; } = expected;

    /// <summary>
    /// Actual number of parameters supplied
    /// </summary>
    public int Actual { get; } = actual;
}