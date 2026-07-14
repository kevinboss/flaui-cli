namespace FlauiCli.Core;

/// <summary>The outcome of one command, carrying the status header and the post-action snapshot.</summary>
public sealed class CommandResult
{
    public bool Ok { get; init; } = true;
    public string? Error { get; init; }

    /// <summary>Short human note about what ran, e.g. "launched notepad.exe".</summary>
    public string? Note { get; init; }

    public string? WindowRef { get; init; }
    public string? WindowTitle { get; init; }
    public string? FocusedRef { get; init; }
    public string? FocusedName { get; init; }
    public string? Snapshot { get; init; }

    /// <summary>A single scalar result (used by --raw for commands that return one value).</summary>
    public string? Value { get; init; }

    public static CommandResult Fail(string error) => new() { Ok = false, Error = error };
}
