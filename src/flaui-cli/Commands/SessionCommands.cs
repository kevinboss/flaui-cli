using FlauiCli.Core;
using FlauiCli.Ipc;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class ListCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var sessions = DaemonClient.ListSessions();
        if (sessions.Count == 0)
        {
            Console.WriteLine("No running sessions.");
            return 0;
        }
        Console.WriteLine("Running sessions:");
        foreach (var s in sessions) Console.WriteLine($"  - {s}");
        return 0;
    }
}

public sealed class CloseCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var session = settings.ResolveSession();
        var response = DaemonClient.SendIfRunning(session, new DaemonRequest { Cmd = "shutdown" });
        Console.WriteLine(response is null ? $"Session '{session}' is not running." : $"Closed session '{session}'.");
        return 0;
    }
}

public sealed class CloseAllCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var sessions = DaemonClient.ListSessions();
        foreach (var s in sessions)
            DaemonClient.SendIfRunning(s, new DaemonRequest { Cmd = "shutdown" });
        Console.WriteLine($"Closed {sessions.Count} session(s).");
        return 0;
    }
}

public sealed class KillAllCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var killed = DaemonClient.KillAll();
        Console.WriteLine($"Killed {killed} daemon process(es).");
        return 0;
    }
}
