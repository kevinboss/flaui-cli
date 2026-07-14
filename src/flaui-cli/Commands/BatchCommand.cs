using System.ComponentModel;
using System.Text.Json;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class BatchSettings : GlobalSettings
{
    [CommandOption("--file <PATH>")]
    [Description("JSON file with an array of command objects. Reads stdin if omitted.")]
    public string? File { get; set; }
}

/// <summary>
/// Run a sequence of commands in one round-trip. Input is a JSON array of request objects, e.g.
/// [{"cmd":"fill","ref":"w1e2","text":"hi"},{"cmd":"click","ref":"w1e5"},{"cmd":"snapshot"}].
/// Stops at the first failure and returns that result; otherwise returns the last.
/// </summary>
public sealed class BatchCommand : Command<BatchSettings>
{
    public override int Execute(CommandContext context, BatchSettings settings)
    {
        var json = settings.File is not null ? System.IO.File.ReadAllText(settings.File) : Console.In.ReadToEnd();
        List<DaemonRequest>? items;
        try { items = JsonSerializer.Deserialize<List<DaemonRequest>>(json, Protocol.Json); }
        catch (Exception ex) { Console.Error.WriteLine($"Error: invalid batch JSON: {ex.Message}"); return 1; }

        var request = new DaemonRequest { Cmd = "batch", Batch = items ?? [] };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}
