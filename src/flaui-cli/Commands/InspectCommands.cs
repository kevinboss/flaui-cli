using System.ComponentModel;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class GetSettings : GlobalSettings
{
    [CommandArgument(0, "<REF>")]
    [Description("Element ref.")]
    public string Ref { get; set; } = "";

    [CommandArgument(1, "[PROPERTY]")]
    [Description("name | value | automationId | controlType | className | helpText | enabled | offscreen | bounds. Omit for all.")]
    public string? Property { get; set; }
}

public sealed class GetCommand : Command<GetSettings>
{
    public override int Execute(CommandContext context, GetSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "get", Ref = settings.Ref, Property = settings.Property }));
}

public sealed class ScreenshotSettings : GlobalSettings
{
    [CommandArgument(0, "[REF]")]
    [Description("Capture this element instead of the active window.")]
    public string? Ref { get; set; }

    [CommandOption("--filename <PATH>")]
    [Description("Output PNG path. Defaults to %LOCALAPPDATA%\\flaui-cli\\shots.")]
    public string? Filename { get; set; }
}

public sealed class ScreenshotCommand : Command<ScreenshotSettings>
{
    public override int Execute(CommandContext context, ScreenshotSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "screenshot", Ref = settings.Ref, Filename = settings.Filename }));
}

public sealed class WindowsCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "windows" }));
}

public sealed class WindowSelectCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "window-select", Ref = settings.Ref }));
}

public sealed class WaitSettings : RefSettings
{
    [CommandOption("--timeout <MS>")]
    [Description("Timeout in milliseconds (default 5000).")]
    public int? TimeoutMs { get; set; }
}

public sealed class WaitCommand : Command<WaitSettings>
{
    public override int Execute(CommandContext context, WaitSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "wait", Ref = settings.Ref, TimeoutMs = settings.TimeoutMs }));
}
