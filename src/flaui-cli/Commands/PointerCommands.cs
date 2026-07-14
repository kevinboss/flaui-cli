using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class DoubleClickCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "dblclick", Ref = settings.Ref }));
}

public sealed class RightClickCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "rightclick", Ref = settings.Ref }));
}

public sealed class HoverCommand : Command<RefSettings>
{
    public override int Execute(CommandContext context, RefSettings settings) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "hover", Ref = settings.Ref }));
}
