using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace FlauiCli.Agent;

/// <summary>
/// Serves one request per pipe connection from a background thread and runs each on the app's UI
/// thread. Snapshots run at Background priority, below Input, so a click an automation peer queued
/// asynchronously has been processed before the tree is read back.
/// </summary>
internal static class AgentServer
{
    private static readonly TimeSpan DispatchTimeout = TimeSpan.FromSeconds(15);

    public static void Start() => new Thread(Serve) { IsBackground = true, Name = "flaui-cli-agent" }.Start();

    private static void Serve()
    {
        var pipeName = AgentProtocol.PipeName(Environment.ProcessId);
        while (true)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
                pipe.WaitForConnection();
                var reader = new StreamReader(pipe);
                var writer = new StreamWriter(pipe) { AutoFlush = true };
                var line = reader.ReadLine();
                if (line is null) continue;
                writer.WriteLine(JsonSerializer.Serialize(Handle(line), AgentProtocol.Json));
                try { pipe.WaitForPipeDrain(); } catch (IOException) { }
            }
            catch (IOException)
            {
            }
        }
    }

    private static AgentResponse Handle(string line)
    {
        AgentRequest? request;
        try { request = JsonSerializer.Deserialize<AgentRequest>(line, AgentProtocol.Json); }
        catch (JsonException ex) { return AgentResponse.Fail("invalid request: " + ex.Message); }
        if (request is null) return AgentResponse.Fail("empty request");
        if (request.Cmd == "ping") return new AgentResponse { Value = Application.Current is null ? "starting" : "ready" };

        var app = Application.Current;
        if (app is null) return AgentResponse.Fail("No WPF Application in this process (in-process mode supports WPF apps only).");

        var priority = request.Cmd == "tree" ? DispatcherPriority.Background : DispatcherPriority.Normal;
        try
        {
            return app.Dispatcher.Invoke(() => WpfAutomation.Run(request), priority, CancellationToken.None, DispatchTimeout);
        }
        catch (TimeoutException)
        {
            return AgentResponse.Fail($"The app's UI thread did not respond within {DispatchTimeout.TotalSeconds:0} s.");
        }
        catch (Exception ex)
        {
            return AgentResponse.Fail(ex.GetBaseException().Message);
        }
    }
}
