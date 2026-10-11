using System.ComponentModel;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class LaunchSettings : GlobalSettings
{
    [CommandArgument(0, "<EXE>")]
    [Description("Executable to launch, e.g. notepad.exe.")]
    public string Exe { get; set; } = "";

    [CommandArgument(1, "[ARGS]")]
    [Description("Arguments passed to the executable.")]
    public string[] Args { get; set; } = [];

    [CommandOption("--inproc")]
    [Description("Load the in-process agent (WPF apps on .NET 8+): p-refs that work in the background and see UI the automation tree hides.")]
    public bool Inproc { get; set; }

    [CommandOption("--minimized")]
    [Description("Start the app minimized.")]
    public bool Minimized { get; set; }
}

public sealed class LaunchCommand : Command<LaunchSettings>
{
    public override int Execute(CommandContext context, LaunchSettings settings, CancellationToken cancellationToken)
    {
        var request = new DaemonRequest { Cmd = "launch", Exe = settings.Exe, Args = string.Join(' ', settings.Args), Inproc = settings.Inproc, Minimized = settings.Minimized };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}

public sealed class AttachSettings : GlobalSettings
{
    [CommandOption("--pid <PID>")]
    [Description("Attach to a running process by id.")]
    public int? Pid { get; set; }

    [CommandOption("--process <NAME>")]
    [Description("Attach to a running process by executable name.")]
    public string? Process { get; set; }

    [CommandOption("--title <REGEX>")]
    [Description("Attach to the first top-level window whose title matches this regex.")]
    public string? Title { get; set; }
}

public sealed class AttachCommand : Command<AttachSettings>
{
    public override int Execute(CommandContext context, AttachSettings settings, CancellationToken cancellationToken)
    {
        DaemonRequest request;
        if (settings.Pid is { } pid) request = new DaemonRequest { Cmd = "attach-pid", Pid = pid };
        else if (!string.IsNullOrWhiteSpace(settings.Process)) request = new DaemonRequest { Cmd = "attach-process", Process = settings.Process };
        else if (!string.IsNullOrWhiteSpace(settings.Title)) request = new DaemonRequest { Cmd = "attach-title", Title = settings.Title };
        else
        {
            Console.Error.WriteLine("Error: pass one of --pid, --process, or --title.");
            return 1;
        }
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}
