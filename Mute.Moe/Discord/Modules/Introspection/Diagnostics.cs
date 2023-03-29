using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Introspection;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
public class Diagnostics
    : BaseModule
{
    private readonly Status _status;

    public Diagnostics(Status status)
    {
        _status = status;
    }

    [Command("memory"), RequireOwner, Summary("I will tell you my current memory usage")]
    public async Task MemoryUsage()
    {
        await ReplyAsync(new EmbedBuilder()
            .AddField("Working Set", _status.MemoryWorkingSet.Bytes().Humanize("#.##"), true)
            .AddField("GC Total Memory", _status.TotalGCMemory.Bytes().Humanize("#.##"), true)
        );
    }

    [Command("hostinfo"), RequireOwner, Summary("I Will tell you where I am being hosted")]
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
    public async Task Ping()
    {
        await ReplyAsync("pong");
    }

    [Command("pong"), Summary("I will respond with 'ping'"), Hidden]
    public async Task Pong()
    {
        await ReplyAsync("ping");
    }

    [Command("latency"), Summary("I will respond with the server latency")]
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
    public async Task Home()
    {
        await TypingReplyAsync("My code is here: https://github.com/martindevans/Mute");
    }

    [Command("shard"), Summary("I will tell you what shard ID I have")]
    public async Task Shard()
    {
        await TypingReplyAsync($"Hello from shard {_status.Shard}");
    }

    [Command("uptime"), Summary("I will tell you how long I have been running")]
    public async Task Uptime()
    {
        await TypingReplyAsync(_status.Uptime.Humanize(2));
    }
}