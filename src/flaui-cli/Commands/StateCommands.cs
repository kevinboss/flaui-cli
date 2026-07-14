using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class FocusCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "focus", Ref = settings.Ref }));
}

public sealed class ToggleCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "toggle", Ref = settings.Ref }));
}

public sealed class CheckCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "check", Ref = settings.Ref }));
}

public sealed class UncheckCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "uncheck", Ref = settings.Ref }));
}

public sealed class SelectCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "select", Ref = settings.Ref }));
}

public sealed class ExpandCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "expand", Ref = settings.Ref }));
}

public sealed class CollapseCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "collapse", Ref = settings.Ref }));
}
