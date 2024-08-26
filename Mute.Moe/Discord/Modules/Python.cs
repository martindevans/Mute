using System.IO;
using System.Text;
using JetBrains.Annotations;
using System.Threading.Tasks;
using Serpent;
using Wasmtime;
using Wazzy.WasiSnapshotPreview1.FileSystem.Implementations.VirtualFileSystem.Files;
using Discord.Commands;

namespace Mute.Moe.Discord.Modules;

public class Python
    : BaseModule
{
    private readonly Engine _engine;

    public Python(Engine engine)
    {
        _engine = engine;
    }

    [Command("python"), Summary("I will run a block of Python code and print the output")]
    [UsedImplicitly]
    public async Task ExecutePython([Remainder] string code)
    {
        if (!(code.StartsWith("```python") || code.StartsWith("```")) || !code.EndsWith("```"))
        {
            await ReplyAsync("Code block must be formatted with triple backticks:\n\\`\\`\\`python\ncode goes here\n\\`\\`\\`");
            return;
        }

        if (code.StartsWith("```python"))
            code = code.Substring(9, code.Length - 12);
        else if (code.StartsWith("```"))
            code = code.Substring(3, code.Length - 6);
        else
            throw new InvalidOperationException("Unknown code formatting");
        

        using var module = new PythonBuilder(_engine);

        var stdOut = new MemoryStream();
        module.WithStdOut(() => new InMemoryFile(0, ReadOnlySpan<byte>.Empty, stdOut));
        var stdErr = new MemoryStream();
        module.WithStdErr(() => new InMemoryFile(0, ReadOnlySpan<byte>.Empty, stdErr));

        var runner = module.Build(Encoding.UTF8.GetBytes(code));

        var message = await ReplyAsync("Executing...");
        var initialFuel = runner.Fuel;
        var ticks = 1;
        try
        {
            runner.Execute();
            while (runner.IsSuspended)
            {
                runner.Execute();
                await Task.Delay(128);
                ticks++;
            }
        }
        catch (TrapException ex)
        {
            await message.ModifyAsync(props =>
            {
                props.Content = $"Execution failed! Trap: `{ex.Type}`";
            });
            return;
        }

        stdOut.Position = 0;
        var output = Encoding.UTF8.GetString(stdOut.ToArray());
        stdErr.Position = 0;
        var error = Encoding.UTF8.GetString(stdErr.ToArray());

        var fuelUsed = initialFuel - runner.Fuel;
        await message.ModifyAsync(props =>
        {
            props.Content = $"Execution finished! {ticks:n0} ticks. {fuelUsed:n0} fuel consumed.";
        });

        if (error.Length > 0)
            await ReplyAsync($"Error:\n```{error}```");
        if (output.Length > 0)
            await ReplyAsync($"Output:\n```{output}```");

    }
}