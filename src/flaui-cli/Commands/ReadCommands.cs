using System.ComponentModel;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class SnapshotSettings : GlobalSettings
{
    [CommandArgument(0, "[REF]")]
    [Description("Snapshot this element's subtree instead of the whole active window.")]
    public string? Ref { get; set; }

    [CommandOption("--depth <DEPTH>")]
    [Description("Maximum tree depth to render.")]
    public int? Depth { get; set; }
}

public sealed class SnapshotCommand : Command<SnapshotSettings>
{
    public override int Execute(CommandContext context, SnapshotSettings settings, CancellationToken cancellationToken)
    {
        var request = new DaemonRequest { Cmd = "snapshot", Ref = settings.Ref, Depth = settings.Depth };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}

public sealed class DesktopSettings : GlobalSettings
{
    [CommandOption("--depth <DEPTH>")]
    [Description("Maximum tree depth to render (default 3).")]
    public int? Depth { get; set; }
}

public sealed class DesktopCommand : Command<DesktopSettings>
{
    public override int Execute(CommandContext context, DesktopSettings settings, CancellationToken cancellationToken)
    {
        var request = new DaemonRequest { Cmd = "desktop", Depth = settings.Depth };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}
