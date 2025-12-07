using System.Threading.Tasks;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Discord.Attributes;

/// <summary>
/// Base class for attributes which can be applied to commands. Attribute will receive a call when the command starts executing and when it finishes.
/// </summary>
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

/// <summary>
/// Disposes something when <see cref="IEndExecute.EndExecute"/> is called
/// </summary>
/// <param name="disposable"></param>
public class DisposableEnd(IDisposable disposable)
    : IEndExecute
{
    /// <inheritdoc />
    public async Task EndExecute()
    {
        disposable.Dispose();
    }
}

/// <summary>
/// Receives a callback when a command completes execution
/// </summary>
public interface IEndExecute
{
    /// <summary>
    /// Called when command finishes
    /// </summary>
    /// <returns></returns>
    Task EndExecute();
}