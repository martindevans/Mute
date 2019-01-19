using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Services.Responses.Eliza.Engine
{
    public interface IReassembly
    {
        /// <summary>
        /// Generate a response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="decomposition">The values pulled out from the input string by the decomposition pattern</param>
        /// <returns></returns>
        Task<string> Rule(ICommandContext context, IReadOnlyList<string> decomposition);
    }

    public class ConstantReassembly
        : IReassembly
    {
        private readonly string _value;

        public ConstantReassembly(string value)
        {
            _value = value;
        }

        [NotNull]
        public Task<string> Rule(ICommandContext _, IReadOnlyList<string> __) => Task.FromResult(_value);
    }

    public class FuncReassembly
        : IReassembly
    {
        private readonly Func<ICommandContext, IReadOnlyList<string>, Task<string>> _func;

        public FuncReassembly([NotNull] Func<ICommandContext, IReadOnlyList<string>, Task<string>> func)
        {
            _func = func;
        }

        public Task<string> Rule(ICommandContext context, IReadOnlyList<string> decomposition) => _func(context, decomposition);
    }
}
