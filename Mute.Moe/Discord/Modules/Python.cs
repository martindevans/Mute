using System.IO;
using System.Text;
using JetBrains.Annotations;
using System.Threading.Tasks;
using Serpent;
using Wasmtime;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules;

public class Python(PythonBuilder _builder)
    : BaseModule
{
    [Command("python"), Summary("I will run a block of Python code and print the output")]
    [UsedImplicitly]
    [RateLimit("776B21EF-257D-4718-904B-D44A7D25EED9", 1, "Please wait a second")]
    public async Task ExecutePython([Remainder] string code)
    {
        // Print a message, we'll edit it later with updates
        var statusMsg = await ReplyAsync("Initialising...");

        // Create runtime
        var stdOutStream = new MemoryStream();
        var stdErrStream = new MemoryStream();
        using var python = CreateRuntime(code, stdOutStream, stdErrStream, out var createErr);
        if (createErr != null || python == null)
        {
            await statusMsg.ModifyAsync(props => props.Content = createErr ?? "Unknown error initialising runtime");
            return;
        }

        // Create a message to hold standard-out
        var stdoutMsg = await ReplyAsync("Output:");

        // Execute
        await SetStatus("Executing...");
        var initialFuel = python.Fuel;
        var ticks = 1;
        var delay = 1;
        var printedLength = 0L;
        var lastPrintTime = DateTime.UtcNow;
        try
        {
            python.Execute();
            while (python.IsSuspended)
            {
                python.Execute();

                // Wait a bit between ticks, exponentially backing off to max limit, then resetting
                await Task.Delay(delay);
                delay *= 2;
                if (delay > 32)
                    delay = 1;
                ticks++;

                // Occasionally update status (less and less often as the program runs longer)
                if (ticks.IsPowerOfTwo())
                {
                    var totalFuelConsumed = initialFuel - python.Fuel;
                    await SetStatus($"Executing... {ticks:n0} ticks. {totalFuelConsumed:n0} fuel consumed.");
                }

                // Print output when there is more available and some time has elapsed since the last print
                if (printedLength != stdOutStream.Length && DateTime.UtcNow - lastPrintTime > TimeSpan.FromSeconds(0.5f))
                {
                    printedLength = await SetStdOut(stdOutStream);
                    lastPrintTime = DateTime.UtcNow;
                }
            }
        }
        catch (TrapException ex)
        {
            await SetStatus($"Execution failed! Trap: `{ex.Type}`");
            return;
        }

        // Final update of stdout
        await SetStdOut(stdOutStream);

        // Show stderr
        if (stdErrStream.Length > 0)
            await ReplyStdErr(stdErrStream);

        // Print final status
        var fuelUsed = initialFuel - python.Fuel;
        await statusMsg.ModifyAsync(props =>
        {
            props.Content = ticks > 1
                          ? $"Execution finished! {ticks:n0} ticks. {fuelUsed:n0} fuel consumed."
                          : $"Execution finished! {fuelUsed:n0} fuel consumed.";
        });
        


        async Task<long> SetStdOut(MemoryStream stdOut)
        {
            // Get memory stream as string
            var p = stdOut.Position;
            stdOut.Position = 0;
            var outputStr = Encoding.UTF8.GetString(stdOut.ToArray());
            stdOut.Position = p;

            // Convert into sensible message
            var message = "Output:\n" + outputStr;
            if (message.Length > 1900)
            {
                var removed = message.Length - 1900;
                message = $"(output too long, {removed} characters removed from start)... " + message[^1900..];
            }

            await stdoutMsg.ModifyAsync(props => { props.Content = $"```{message}```"; });

            return stdOut.Length;
        }

        async Task ReplyStdErr(MemoryStream stdErr)
        {
            // Get memory stream as string
            var p = stdErr.Position;
            stdErr.Position = 0;
            var message = Encoding.UTF8.GetString(stdErr.ToArray());
            stdErr.Position = p;

            if (message.Length > 1900)
            {
                var removed = message.Length - 1900;
                message = message[..1900] + $"... (output too long, {removed} removed from end)";
            }

            await ReplyAsync($"```{message}```");
        }

        async Task SetStatus(string message)
        {
            if (message.Length > 1900)
            {
                var removed = message.Length - 1900;
                message = message[..1900] + $"... (output too long, {removed} removed from end)";
            }

            await statusMsg.ModifyAsync(props => { props.Content = message; });
        }
    }

    private Serpent.Python? CreateRuntime(string code, MemoryStream stdOut, MemoryStream stdErr, out string? error)
    {
        // Sanity check formatting
        if (!(code.StartsWith("```python") || code.StartsWith("```")) || !code.EndsWith("```"))
        {
            error = "Code block must be formatted with triple backticks:\n\\`\\`\\`python\ncode goes here\n\\`\\`\\`";
            return default;
        }

        // Strip off code block
        if (code.StartsWith("```python"))
            code = code.Substring(9, code.Length - 12);
        else if (code.StartsWith("```"))
            code = code.Substring(3, code.Length - 6);
        else
            throw new InvalidOperationException("Unknown code formatting");

        // Construct runtime
        var runner = _builder
            .Create()
            .WithStdOut(() => new InMemoryFile(0, [ ], stdOut))
            .WithStdErr(() => new InMemoryFile(0, [ ], stdErr))
            .WithCode(Encoding.UTF8.GetBytes(code))
            .Build();

        error = default;
        return runner;
    }
}