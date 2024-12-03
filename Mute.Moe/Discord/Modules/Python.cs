using System.IO;
using System.Text;
using JetBrains.Annotations;
using System.Threading.Tasks;
using Serpent;
using Wasmtime;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;

namespace Mute.Moe.Discord.Modules;

public class Python(PythonBuilder _builder)
    : BaseModule
{
    [Command("python"), Summary("I will run a block of Python code and print the output")]
    [UsedImplicitly]
    [RateLimit("776B21EF-257D-4718-904B-D44A7D25EED9", 1, "Please wait a second")]
    public async Task ExecutePython([Remainder] string code)
    {
        // Sanity check formatting
        if (!(code.StartsWith("```python") || code.StartsWith("```")) || !code.EndsWith("```"))
        {
            await ReplyAsync("Code block must be formatted with triple backticks:\n\\`\\`\\`python\ncode goes here\n\\`\\`\\`");
            return;
        }

        // Strip off code block
        if (code.StartsWith("```python"))
            code = code.Substring(9, code.Length - 12);
        else if (code.StartsWith("```"))
            code = code.Substring(3, code.Length - 6);
        else
            throw new InvalidOperationException("Unknown code formatting");

        // Print a message, we'll edit it later with updates
        var message = await ReplyAsync("Initialising...");

        // Construct runtime
        var stdOut = new MemoryStream();
        var stdErr = new MemoryStream();
        using var runner = _builder.Create()
              .WithStdOut(() => new InMemoryFile(0, ReadOnlySpan<byte>.Empty, stdOut))
              .WithStdErr(() => new InMemoryFile(0, ReadOnlySpan<byte>.Empty, stdErr))
              .WithCode(Encoding.UTF8.GetBytes(code))
              .Build();

        // Execute
        await message.ModifyAsync(props => { props.Content = "Executing..."; });
        var initialFuel = runner.Fuel;
        var ticks = 1;
        try
        {
            runner.Execute();
            while (runner.IsSuspended)
            {
                var fuelBefore = runner.Fuel;
                runner.Execute();
                var fuelAfter = runner.Fuel;
                var fuelConsumed = fuelBefore - fuelAfter;

                // If not much fuel was consumed then the code is probably in a loop
                // polling until an async event happens. Use a longer delay in this case.
                if (fuelConsumed < 50000)
                    await Task.Delay(200);
                else
                    await Task.Delay(10);
                ticks++;
            }
        }
        catch (TrapException ex)
        {
            await message.ModifyAsync(props => { props.Content = $"Execution failed! Trap: `{ex.Type}`"; });
            return;
        }

        // Extract output
        stdOut.Position = 0;
        var output = Encoding.UTF8.GetString(stdOut.ToArray());
        stdErr.Position = 0;
        var error = Encoding.UTF8.GetString(stdErr.ToArray());

        // Print results
        var fuelUsed = initialFuel - runner.Fuel;

        await message.ModifyAsync(props =>
        {
            props.Content = ticks > 1
                          ? $"Execution finished! {ticks:n0} ticks. {fuelUsed:n0} fuel consumed."
                          : $"Execution finished! {fuelUsed:n0} fuel consumed.";
        });
        
        if (error.Length > 0)
            await ReplyAsync($"Error:\n```\n{error}```");
        if (output.Length > 0)
            await ReplyAsync($"Output:\n```\n{output}```");
    }
}