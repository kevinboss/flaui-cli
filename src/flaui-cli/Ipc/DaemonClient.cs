using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;
using FlauiCli.Core;

namespace FlauiCli.Ipc;

/// <summary>
/// The thin client half. Connects to a session's daemon over its named pipe, spawning the daemon
/// detached on first use, and exchanges one request/response per call.
/// </summary>
internal static class DaemonClient
{
    /// <summary>Send a command, starting the daemon if it isn't already running.</summary>
    public static DaemonResponse Send(string session, DaemonRequest request)
        => Exchange(session, request, autoStart: true)
           ?? new DaemonResponse { Ok = false, Error = "no response from daemon" };

    /// <summary>Send only if a daemon is already running; returns null if none is.</summary>
    public static DaemonResponse? SendIfRunning(string session, DaemonRequest request)
        => Exchange(session, request, autoStart: false);

    private static DaemonResponse? Exchange(string session, DaemonRequest request, bool autoStart)
    {
        var pipeName = Protocol.PipeName(session);
        var client = TryConnect(pipeName, TimeSpan.FromMilliseconds(300));
        if (client is null)
        {
            if (!autoStart) return null;
            SpawnDaemon(session, pipeName);
            client = ConnectWithRetry(pipeName, TimeSpan.FromSeconds(20));
        }

        using (client)
        {
            var writer = new StreamWriter(client) { AutoFlush = true };
            var reader = new StreamReader(client);
            writer.WriteLine(JsonSerializer.Serialize(request, Protocol.Json));
            var line = reader.ReadLine();
            return line is null ? null : JsonSerializer.Deserialize<DaemonResponse>(line, Protocol.Json);
        }
    }

    private static NamedPipeClientStream? TryConnect(string pipeName, TimeSpan timeout)
    {
        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            client.Connect((int)timeout.TotalMilliseconds);
            return client;
        }
        catch (Exception ex) when (ex is TimeoutException or IOException)
        {
            client.Dispose();
            return null;
        }
    }

    private static NamedPipeClientStream ConnectWithRetry(string pipeName, TimeSpan overall)
    {
        var deadline = DateTime.UtcNow + overall;
        while (DateTime.UtcNow < deadline)
        {
            var client = TryConnect(pipeName, TimeSpan.FromMilliseconds(250));
            if (client is not null) return client;
            Thread.Sleep(100);
        }
        throw new IOException($"Timed out waiting for the daemon on pipe '{pipeName}'.");
    }

    private static void SpawnDaemon(string session, string pipeName)
    {
        var (fileName, prefix) = SelfInvocation();
        NativeSpawn.SpawnDetached(fileName, $"{prefix}--daemon --session \"{session}\" --pipe \"{pipeName}\"");
    }

    /// <summary>
    /// How to relaunch this program to host a daemon. A published single-file build is its own
    /// exe; under `dotnet run` the host is dotnet, so we re-invoke `dotnet &lt;dll&gt;`.
    /// </summary>
    private static (string fileName, string argsPrefix) SelfInvocation()
    {
        var processPath = Environment.ProcessPath ?? "";
        var processName = Path.GetFileNameWithoutExtension(processPath);
        if (string.Equals(processName, "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var dll = Assembly.GetEntryAssembly()?.Location ?? "";
            return (processPath, $"\"{dll}\" ");
        }
        return (processPath, "");
    }

    // ---- session management ------------------------------------------------

    public static IReadOnlyList<string> ListSessions()
    {
        try
        {
            return Directory.EnumerateFiles(@"\\.\pipe\")
                .Select(Path.GetFileName)
                .Where(n => n is not null && n.StartsWith(Protocol.PipePrefix, StringComparison.Ordinal))
                .Select(n => n![Protocol.PipePrefix.Length..])
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception ex)
        {
            if (Environment.GetEnvironmentVariable("FLAUI_CLI_DEBUG") == "1")
                Console.Error.WriteLine($"[list] pipe enumeration failed: {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    /// <summary>Force-kill every daemon process (this client excluded).</summary>
    public static int KillAll()
    {
        var self = Environment.ProcessId;
        var killed = 0;
        foreach (var p in Process.GetProcessesByName("flaui-cli"))
        {
            if (p.Id == self) continue;
            try { p.Kill(); killed++; } catch { /* already gone or access denied */ }
            finally { p.Dispose(); }
        }
        return killed;
    }
}
