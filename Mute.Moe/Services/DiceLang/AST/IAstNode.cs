using System;
using System.Text.Json.Serialization;
using Mute.Moe.Services.Randomness;

namespace Mute.Moe.Services.DiceLang.AST;

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
    public double Evaluate(IDiceRoller roller, IMacroResolver resolver)
    {
        return Evaluate(new Context
        {
            Roller = roller,
            MacroResolver = resolver,
            NamedArgs = new Dictionary<string, IAstNode>(),
        });
    }

    public double Evaluate(Context context);

    public IAstNode Reduce();

    protected static IAstNode BinaryReduce(IAstNode left, IAstNode right, Func<IAstNode, IAstNode, IAstNode> cons)
    {
        var l = left.Reduce();
        var r = right.Reduce();

        var node = cons(l, r);

        if (l is ConstantValue && r is ConstantValue)
            return new ConstantValue(node.Evaluate(default));

        return node;
    }

    readonly record struct Context(IDiceRoller Roller, IMacroResolver MacroResolver, IReadOnlyDictionary<string, IAstNode> NamedArgs);
}