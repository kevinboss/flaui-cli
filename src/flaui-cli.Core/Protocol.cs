using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlauiCli.Core;

/// <summary>Wire contract between the thin CLI client and the per-session daemon.</summary>
public static class Protocol
{
    public const string PipePrefix = "flaui-cli-";

    public static string PipeName(string session) => PipePrefix + session;

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>A single flat request; only the fields relevant to <see cref="Cmd"/> are set.</summary>
public sealed class DaemonRequest
{
    public string Cmd { get; set; } = "";
    public string? Ref { get; set; }
    public string? Text { get; set; }
    public string? Exe { get; set; }
    public string? Args { get; set; }
    public int? Pid { get; set; }
    public string? Process { get; set; }
    public string? Title { get; set; }
    public string? Key { get; set; }
    public int? Depth { get; set; }
    public bool Submit { get; set; }
}

/// <summary>The daemon's reply: a serialized <see cref="CommandResult"/>.</summary>
public sealed class DaemonResponse
{
    public bool Ok { get; set; } = true;
    public string? Error { get; set; }
    public string? Note { get; set; }
    public string? WindowRef { get; set; }
    public string? WindowTitle { get; set; }
    public string? FocusedRef { get; set; }
    public string? FocusedName { get; set; }
    public string? Snapshot { get; set; }
    public string? Value { get; set; }

    public static DaemonResponse From(CommandResult r) => new()
    {
        Ok = r.Ok,
        Error = r.Error,
        Note = r.Note,
        WindowRef = r.WindowRef,
        WindowTitle = r.WindowTitle,
        FocusedRef = r.FocusedRef,
        FocusedName = r.FocusedName,
        Snapshot = r.Snapshot,
        Value = r.Value,
    };
}
