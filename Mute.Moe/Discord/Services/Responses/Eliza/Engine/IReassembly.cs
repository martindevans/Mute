using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;


namespace Mute.Moe.Discord.Services.Responses.Eliza.Engine;

public interface IReassembly
{
    /// <summary>
    /// Generate a response assembly string. Parts of the input will be substituted into the output by using `(n)` where `n` is the index of the input part.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="decomposition">The values pulled out from the input string by the decomposition pattern</param>
    /// <returns></returns>
    Task<string?> Assemble(ICommandContext context, IReadOnlyList<string> decomposition);
}

public class ConstantReassembly
    : IReassembly
{
    private readonly string _value;

    public ConstantReassembly(string value)
    {
        _value = value;
    }

        
    public Task<string?> Assemble(ICommandContext _, IReadOnlyList<string> __) => Task.FromResult<string?>(_value);
}

public class FuncReassembly
    : IReassembly
{
    private readonly Func<ICommandContext, IReadOnlyList<string>, Task<string?>> _func;

    public FuncReassembly(Func<ICommandContext, IReadOnlyList<string>, Task<string?>> func)
    {
        _func = func;
    }

    public Task<string?> Assemble(ICommandContext context, IReadOnlyList<string> decomposition) => _func(context, decomposition);
}