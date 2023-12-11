using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Introspection;
using Mute.Moe.Services.Introspection.Uptime;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
public class Diagnostics
    : BaseModule
{
    private readonly IUptime _uptime;
    private readonly Status _status;

    public Diagnostics(IUptime uptime, Status status)
    {
        _uptime = uptime;
        _status = status;
    }

    [Command("memory"), RequireOwner, Summary("I will tell you my current memory usage")]
    [UsedImplicitly]
    public async Task MemoryUsage()
    {
        await ReplyAsync(new EmbedBuilder()
            .AddField("Working Set", _status.MemoryWorkingSet.Bytes().Humanize("#.##"), true)
            .AddField("GC Total Memory", _status.TotalGCMemory.Bytes().Humanize("#.##"), true)
        );
    }

    [Command("hostinfo"), RequireOwner, Summary("I will tell you where I am being hosted")]
    [UsedImplicitly]
    public async Task HostName()
    {
        var embed = new EmbedBuilder()
            .AddField("Machine", Environment.MachineName)
            .AddField("User", Environment.UserName)
            .AddField("OS", Environment.OSVersion)
            .AddField("CPUs", Environment.ProcessorCount)
            .Build();

        await ReplyAsync("", false, embed);
    }

    [Command("ping"), Summary("I will respond with 'pong'"), Alias("test")]
    [UsedImplicitly]
    public async Task Ping()
    {
        await ReplyAsync("pong");
    }

    [Command("pong"), Summary("I will respond with 'ping'"), Hidden]
    [UsedImplicitly]
    public async Task Pong()
    {
        await ReplyAsync("ping");
    }

    [Command("latency"), Summary("I will respond with the server latency")]
    [UsedImplicitly]
    public async Task Latency()
    {
        var latency = _status.Latency.TotalMilliseconds;

        var message = latency switch
        {
            < 75 => $"My latency is {latency}ms, that's great!",
            < 150 => $"My latency is {latency}ms",
            _ => $"My latency is {latency}ms, that's a bit slow"
        };

        await TypingReplyAsync(message);
    }

    [Command("home"), Summary("I will tell you where to find my source code"), Alias("source", "github")]
    [UsedImplicitly]
    public async Task Home()
    {
        await TypingReplyAsync("My code is here: https://github.com/martindevans/Mute");
    }

    [Command("uptime"), Summary("I will tell you how long I have been running")]
    [UsedImplicitly]
    public async Task Uptime()
    {
        await TypingReplyAsync(_uptime.Uptime.Humanize(2));
    }

    [Command("simd"), RequireOwner]
    [UsedImplicitly]
    public async Task Simd()
    {
        var embed = new EmbedBuilder().WithTitle("SIMD Support").WithDescription(
            $"- AES:  {Aes.IsSupported}\n" +
            $"- AVX:  {Avx.IsSupported}\n" +
            $"- AVX2: {Avx2.IsSupported}\n" +
            $"- BMI1: {Bmi1.IsSupported}\n" +
            $"- BMI2: {Bmi2.IsSupported}\n" +
            $"- FMA:  {Fma.IsSupported}\n" +
            $"- LZCNT:{Lzcnt.IsSupported}\n" +
            $"- PCLMULQDQ:{Pclmulqdq.IsSupported}\n" +
            $"- POPCNT:{Popcnt.IsSupported}\n" +
            $"- SSE:{Sse.IsSupported}\n" +
            $"- SSE2:{Sse2.IsSupported}\n" +
            $"- SSE3:{Sse3.IsSupported}\n" +
            $"- SSSE3:{Ssse3.IsSupported}\n" +
            $"- SSE41:{Sse41.IsSupported}\n" +
            $"- SSE42:{Sse42.IsSupported}\n"
        ).Build();

        await ReplyAsync(embed: embed);
    }
}