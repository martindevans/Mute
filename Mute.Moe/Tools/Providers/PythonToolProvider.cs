using System.IO;
using System.Text;
using System.Threading.Tasks;
using Serpent;
using Wasmtime;
using Wazzy.Async;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides Python code execution (sandboxed).
/// </summary>
public class PythonToolProvider
    : IToolProvider
{
    private readonly PythonBuilder _builder;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create <see cref="PythonToolProvider"/>
    /// </summary>
    /// <param name="builder"></param>
    public PythonToolProvider(PythonBuilder builder)
    {
        _builder = builder;

        Tools =
        [
            new AutoTool("python", true, ExecutePython),
        ];
    }

    /// <summary>
    /// Execute some python code and return the results from stdout and stderr. Code is executed within a WASM sandbox, file system access
    /// is available to a sandboxed in-memory filesystem, network access is not available. Execution consumes "fuel" and will be cancelled
    /// if all fuel is consumed, so infinite loops are safe.
    /// </summary>
    /// <param name="code">A block of python code to execute</param>
    /// <returns></returns>
    private async Task<object> ExecutePython(string code)
    {
        // Construct runtime
        var stdOut = new MemoryStream();
        var stdErr = new MemoryStream();
        using var python = _builder
            .Create()
            .WithStdOut(() => new InMemoryFile(0, [], stdOut))
            .WithStdErr(() => new InMemoryFile(0, [], stdErr))
            .WithCode(Encoding.UTF8.GetBytes(code))
            .Build();

        var initialFuel = python.Fuel;
        var delay = 1;
        var startTime = DateTime.UtcNow;
        try
        {
            python.Execute();
            while (python.IsSuspended)
            {
                python.Execute();

                if (python.SuspendedReason is TaskSuspend t)
                {
                    // If WASM is suspended waiting for a dotnet task, await it here
                    await t.Task;
                    delay = 1;
                }
                else
                {
                    // Wait a bit between ticks, exponentially backing off to max limit, then resetting
                    await Task.Delay(delay);
                    delay *= 2;
                    if (delay > 32)
                        delay = 1;
                }
            }
        }
        catch (TrapException ex)
        {
            return new
            {
                Error = $"Execution failed! Trap: `{ex.Type}`"
            };
        }

        var fuelUsed = initialFuel - python.Fuel;

        return new
        {
            FuelUsed = fuelUsed,
            ElapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
            StdOut = MemoryStreamToString(stdOut),
            StdErr = MemoryStreamToString(stdOut)
        };
    }

    private static string MemoryStreamToString(MemoryStream mem)
    {
        var p = mem.Position;
        mem.Position = 0;
        try
        {
            using var reader = new StreamReader(mem, Encoding.UTF8, leaveOpen:true);
            return reader.ReadToEnd();
        }
        finally
        {
            mem.Position = p;
        }
    }
}