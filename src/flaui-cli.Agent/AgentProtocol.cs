using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlauiCli.Agent;

/// <summary>
/// Wire contract between the flaui-cli daemon and the in-process agent. Kept separate from the
/// daemon's own protocol because the agent may not reference flaui-cli.Core (it would drag FlaUI
/// into the target app). The daemon mirrors these shapes.
/// </summary>
public static class AgentProtocol
{
    public static string PipeName(int processId) => $"flaui-cli-agent-{processId}";

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

public sealed class AgentRequest
{
    public string Cmd { get; set; } = "";
    public string? Ref { get; set; }
    public string? Text { get; set; }
    public string? Key { get; set; }
    public int? Depth { get; set; }
}

public sealed class AgentResponse
{
    public bool Ok { get; set; } = true;
    public string? Error { get; set; }
    public string? Value { get; set; }
    public string? Snapshot { get; set; }

    public static AgentResponse Fail(string error) => new() { Ok = false, Error = error };
}
