using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Utilities;

public static class AsyncProcess
{
    public static async Task<int> StartProcess(string filename, string arguments, string workingDirectory, int? timeout = null)
    {
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            Arguments = arguments,
            FileName = filename,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var cancellationTokenSource = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

        await process.WaitForExitAsync(cancellationTokenSource.Token);

        return process.ExitCode;
    }
}