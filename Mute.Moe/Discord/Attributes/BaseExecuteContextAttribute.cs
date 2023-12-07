using System.Threading.Tasks;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

public abstract class BaseExecuteContextAttribute
    : Attribute
{
    /// <summary>
    /// Return an object which will be disposed once the command execution is complete
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected internal abstract IEndExecute StartExecute(MuteCommandContext context);
}

public class DisposableEnd(IDisposable disposable)
    : IEndExecute
{
    public async Task EndExecute()
    {
        disposable.Dispose();
    }
}

public interface IEndExecute
{
    Task EndExecute();
}