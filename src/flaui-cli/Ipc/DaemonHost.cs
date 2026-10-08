using System.IO.Pipes;
using System.Text.Json;
using FlauiCli.Core;

namespace FlauiCli.Ipc;

/// <summary>
/// The per-session daemon. Owns one <see cref="Session"/> (and therefore the UIA worker thread),
/// listens on a named pipe, handles one command per client connection, and shuts itself down
/// after an idle period so stale daemons don't linger.
/// </summary>
internal static class DaemonHost
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(10);

    public static async Task<int> Run(string session, string pipeName)
    {
        using var flaui = new Session(session);
        while (true)
        {
            using var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            using var idle = new CancellationTokenSource(IdleTimeout);
            try { await server.WaitForConnectionAsync(idle.Token); }
            catch (OperationCanceledException) { return 0; } // idle timeout: exit cleanly

            var keepRunning = await Handle(server, flaui);
            try { server.Disconnect(); } catch { /* client may have already gone */ }
            if (!keepRunning) return 0;
        }
    }

    private static async Task<bool> Handle(NamedPipeServerStream server, Session flaui)
    {
        var reader = new StreamReader(server, leaveOpen: true);
        var writer = new StreamWriter(server) { AutoFlush = true };

        var line = await reader.ReadLineAsync();
        if (line is null) return true;

        DaemonRequest? req;
        try { req = JsonSerializer.Deserialize<DaemonRequest>(line, Protocol.Json); }
        catch (Exception ex) { await Reply(server, writer, Fail(ex.Message)); return true; }
        if (req is null) { await Reply(server, writer, Fail("empty request")); return true; }

        switch (req.Cmd)
        {
            case "ping":
                await Reply(server, writer, new DaemonResponse { Ok = true, Note = "pong" });
                return true;
            case "shutdown":
                await Reply(server, writer, new DaemonResponse { Ok = true, Note = "bye" });
                return false;
            default:
                var result = Dispatch(flaui, req);
                await Reply(server, writer, DaemonResponse.From(result));
                return true;
        }
    }

    private static CommandResult Dispatch(Session s, DaemonRequest r) => r.Cmd switch
    {
        "launch" => s.Launch(r.Exe ?? "", r.Args ?? ""),
        "attach-pid" => s.AttachPid(r.Pid ?? 0),
        "attach-process" => s.AttachProcess(r.Process ?? ""),
        "attach-title" => s.AttachTitle(r.Title ?? ""),
        "snapshot" => s.Snapshot(r.Ref, r.Depth ?? SnapshotBuilder.DefaultDepth),
        "desktop" => s.SnapshotDesktop(r.Depth ?? 3),
        "windows" => s.Windows(),
        "window-select" => s.WindowSelect(r.Ref ?? ""),
        "get" => s.Get(r.Ref ?? "", r.Property),
        "screenshot" => s.Screenshot(r.Ref, r.Filename),
        "ocr" => s.Ocr(r.Ref, r.Words, r.Lang),
        "click" => s.Click(r.Ref ?? ""),
        "click-at" => s.ClickAt(r.X ?? 0, r.Y ?? 0, r.Double, r.Right),
        "dblclick" => s.DoubleClick(r.Ref ?? ""),
        "rightclick" => s.RightClick(r.Ref ?? ""),
        "hover" => s.Hover(r.Ref ?? ""),
        "focus" => s.Focus(r.Ref ?? ""),
        "check" => s.Check(r.Ref ?? ""),
        "uncheck" => s.Uncheck(r.Ref ?? ""),
        "toggle" => s.Toggle(r.Ref ?? ""),
        "select" => s.Select(r.Ref ?? ""),
        "expand" => s.Expand(r.Ref ?? ""),
        "collapse" => s.Collapse(r.Ref ?? ""),
        "type" => s.Type(r.Text ?? ""),
        "fill" => s.Fill(r.Ref ?? "", r.Text ?? "", r.Submit),
        "press" => s.Press(r.Key ?? ""),
        "wait" => s.Wait(r.Ref ?? "", r.TimeoutMs ?? 5000),
        "batch" => Batch(s, r),
        _ => CommandResult.Fail($"Unknown command '{r.Cmd}'"),
    };

    /// <summary>Run a sequence of commands in one round-trip, stopping at the first failure.</summary>
    private static CommandResult Batch(Session s, DaemonRequest r)
    {
        CommandResult last = new() { Ok = true, Note = "batch (empty)" };
        foreach (var item in r.Batch ?? [])
        {
            last = Dispatch(s, item);
            if (!last.Ok) return last;
        }
        return last;
    }

    private static async Task Reply(NamedPipeServerStream server, StreamWriter writer, DaemonResponse response)
    {
        await writer.WriteLineAsync(JsonSerializer.Serialize(response, Protocol.Json));
        await writer.FlushAsync();
        try { server.WaitForPipeDrain(); } catch { /* client read and left */ }
    }

    private static DaemonResponse Fail(string error) => new() { Ok = false, Error = error };
}
