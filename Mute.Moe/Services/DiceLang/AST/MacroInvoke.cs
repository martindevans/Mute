using System;
using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record MacroInvoke(string? Namespace, string Name, IReadOnlyList<IAstNode> Arguments)
    : IAstNode
{
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

    public override string ToString()
    {
        var name = string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";
        return $"{name}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
    }
}


public interface IMacroResolver
{
    Task<MacroDefinition?> Find(string? ns, string name);
}

public record MacroDefinition(string Namespace, string Name, IReadOnlyList<string> ParameterNames, IAstNode Root)
{
    public override string ToString()
    {
        return $"`{Namespace}::{Name}({string.Join(", ", ParameterNames)}) = {Root}`";
    }
}



public class MacroNotFoundException
    : Exception
{
    public string? Namespace { get; }
    public string Name { get; set; }

    public MacroNotFoundException(string? ns, string name)
        : base($"Failed to find macro `{ns}::{name}`")
    {
        Namespace = ns;
        Name = name;
    }
}

public class MacroIncorrectArgumentCount
    : Exception
{
    public string? Namespace { get; }
    public string Name { get; set; }
    public int Expected { get; }
    public int Actual { get; }

    public MacroIncorrectArgumentCount(string? ns, string name, int expected, int actual)
        : base($"Expected {expected} arguments for macro `{ns}::{name}`, but given {actual}")
    {
        Namespace = ns;
        Name = name;
        Expected = expected;
        Actual = actual;
    }
}