using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

/// <summary>
/// AST node for dicelang
/// </summary>
[JsonDerivedType(typeof(Add), typeDiscriminator: "Add")]
[JsonDerivedType(typeof(ConstantValue), typeDiscriminator: "ConstantValue")]
[JsonDerivedType(typeof(DiceRollValue), typeDiscriminator: "DiceRollValue")]
[JsonDerivedType(typeof(Divide), typeDiscriminator: "Divide")]
[JsonDerivedType(typeof(Exponent), typeDiscriminator: "Exponent")]
[JsonDerivedType(typeof(MacroInvoke), typeDiscriminator: "MacroInvoke")]
[JsonDerivedType(typeof(Multiply), typeDiscriminator: "Multiply")]
[JsonDerivedType(typeof(Negate), typeDiscriminator: "Negate")]
[JsonDerivedType(typeof(Parameter), typeDiscriminator: "Parameter")]
[JsonDerivedType(typeof(Parens), typeDiscriminator: "Parens")]
[JsonDerivedType(typeof(Subtract), typeDiscriminator: "Subtract")]
public interface IAstNode
{
    /// <summary>
    /// Evaluate this node and produce a result
    /// </summary>
    /// <param name="roller"></param>
    /// <param name="resolver"></param>
    /// <returns></returns>
    public Task<double> Evaluate(IDiceRoller roller, IMacroResolver resolver)
    {
        return Evaluate(new Context
        {
            Roller = roller,
            MacroResolver = resolver,
            NamedArgs = new Dictionary<string, IAstNode>(),
        });
    }

    /// <summary>
    /// Evaluate this node and produce a result
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task<double> Evaluate(Context context);

    /// <summary>
    /// Context for evaluation
    /// </summary>
    /// <param name="Roller"></param>
    /// <param name="MacroResolver"></param>
    /// <param name="NamedArgs"></param>
    readonly record struct Context(IDiceRoller Roller, IMacroResolver MacroResolver, IReadOnlyDictionary<string, IAstNode> NamedArgs);
}