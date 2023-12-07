using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Parameter(string Name)
    : IAstNode
{
    public Task<double> Evaluate(IAstNode.Context context)
    {
        var arg = context.NamedArgs[Name];
        return arg.Evaluate(context);
    }

    public override string ToString()
    {
        return Name;
    }
}