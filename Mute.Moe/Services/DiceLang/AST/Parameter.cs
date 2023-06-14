using System.Threading.Tasks;

namespace Mute.Moe.Services.DiceLang.AST;

public record Parameter(string Name)
    : IAstNode
{
    public async Task<double> Evaluate(IAstNode.Context context)
    {
        var arg = context.NamedArgs[Name];
        return await arg.Evaluate(context);
    }

    public override string ToString()
    {
        return Name;
    }
}