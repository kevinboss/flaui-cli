using System.IO.Pipes;
using System.Text.Json;

namespace FlauiCli.Core;

/// <summary>
/// The daemon's side of the pipe to the in-process agent (src/flaui-cli.Agent). One request per
/// connection, newline-delimited JSON, mirroring the daemon's own client protocol. The message
/// shapes duplicate the agent's on purpose: referencing the agent assembly would pull the WPF
/// framework into the CLI.
/// </summary>
internal sealed class AgentClient(int processId)
{
    private readonly string _pipeName = $"flaui-cli-agent-{processId}";

    public AgentReply Send(AgentCall call, TimeSpan? connectTimeout = null)
    {
        using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
        pipe.Connect((int)(connectTimeout ?? TimeSpan.FromSeconds(5)).TotalMilliseconds);
        var writer = new StreamWriter(pipe) { AutoFlush = true };
        var reader = new StreamReader(pipe);
        writer.WriteLine(JsonSerializer.Serialize(call, Protocol.Json));
        var line = reader.ReadLine() ?? throw new IOException("The in-process agent closed the connection without replying.");
        return JsonSerializer.Deserialize<AgentReply>(line, Protocol.Json) ?? throw new IOException("The in-process agent sent an empty reply.");
    }

    /// <summary>Block until the agent reports the app's WPF Application exists.</summary>
    public void WaitReady(Func<bool> appExited, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (appExited()) throw new InvalidOperationException("The app exited before the in-process agent came up.");
            try
            {
                if (Send(new AgentCall { Cmd = "ping" }, TimeSpan.FromMilliseconds(500)).Value == "ready") return;
            }
            catch (TimeoutException)
            {
            }
            catch (IOException)
            {
            }
            Thread.Sleep(250);
        }
        throw new TimeoutException("The in-process agent did not come up. In-process mode needs a WPF app on .NET 8 or later.");
    }
}

internal sealed class AgentCall
{
    public string Cmd { get; set; } = "";
    public string? Ref { get; set; }
    public string? Text { get; set; }
    public string? Key { get; set; }
    public int? Depth { get; set; }
}

internal sealed class AgentReply
{
    public bool Ok { get; set; } = true;
    public string? Error { get; set; }
    public string? Value { get; set; }
    public string? Snapshot { get; set; }
}
