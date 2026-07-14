using System.ComponentModel;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

/// <summary>Options shared by every command: session routing and output shaping.</summary>
public class GlobalSettings : CommandSettings
{
    [CommandOption("-s|--session <SESSION>")]
    [Description("Session name; isolates one app. Defaults to $FLAUI_CLI_SESSION or 'default'.")]
    public string? Session { get; set; }

    [CommandOption("--raw")]
    [Description("Print only the result value (snapshot or scalar), no status header.")]
    public bool Raw { get; set; }

    [CommandOption("--json")]
    [Description("Print the raw JSON response.")]
    public bool Json { get; set; }

    public string ResolveSession()
    {
        if (!string.IsNullOrWhiteSpace(Session)) return Session;
        var env = Environment.GetEnvironmentVariable("FLAUI_CLI_SESSION");
        return string.IsNullOrWhiteSpace(env) ? "default" : env;
    }
}
