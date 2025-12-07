using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// Fetches a named parameter from the context and evaluates it
/// </summary>
/// <param name="Name"></param>
public record Parameter(string Name)
    : IAstNode
{
    /// <inheritdoc />
    public Task<double> Evaluate(IAstNode.Context context)
    {
        var arg = context.NamedArgs[Name];
        return arg.Evaluate(context);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}