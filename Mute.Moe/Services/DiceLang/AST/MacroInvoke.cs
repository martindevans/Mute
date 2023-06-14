using System;

namespace Mute.Moe.Services.DiceLang.AST;

public record MacroInvoke(string? Namespace, string Name, IReadOnlyList<IAstNode> Arguments)
    : IAstNode
{
    public double Evaluate(IAstNode.Context context)
    {
        // Get the macro
        var macro = context.MacroResolver.Find(Namespace, Name) 
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
        return macro.Root.Evaluate(ctx);
    }

    public IAstNode Reduce()
    {
        return this;
    }

    public override string ToString()
    {
        var name = string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}::{Name}";
        return $"{name}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
    }
}


public interface IMacroResolver
{
    MacroDefinition? Find(string? ns, string name);
}

public record MacroDefinition(IReadOnlyList<string> ParameterNames, IAstNode Root);

public class NullMacroResolver
    : IMacroResolver
{
    public MacroDefinition? Find(string? ns, string name)
    {
        return null;

        // Test definition which adds 2 parameters
        return new MacroDefinition(
            new[] { "x", "y" },
            new Add(new Parameter("x"), new Parameter("y"))
        );
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