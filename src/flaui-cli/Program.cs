using FlauiCli.Commands;
using FlauiCli.Core;
using FlauiCli.Ipc;
using Spectre.Console.Cli;

// Daemon role: host the UIA worker and the pipe server for one session.
// The client spawns this detached; it is never invoked by a human directly.
if (args.Contains("--daemon"))
{
    var session = GetArg("--session") ?? "default";
    var pipe = GetArg("--pipe") ?? Protocol.PipeName(session);
    return await DaemonHost.Run(session, pipe);
}

// Client role: the user-facing CLI.
Console.OutputEncoding = System.Text.Encoding.UTF8;
var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("flaui-cli");
    config.AddCommand<LaunchCommand>("launch").WithDescription("Launch an app and attach automation.");
    config.AddCommand<AttachCommand>("attach").WithDescription("Attach to a running app.");
    config.AddCommand<SnapshotCommand>("snapshot").WithDescription("Snapshot the active window (or a subtree).");
    config.AddCommand<DesktopCommand>("desktop").WithDescription("Snapshot all top-level windows.");
    config.AddCommand<WindowsCommand>("windows").WithDescription("List the app's top-level windows.");
    config.AddCommand<WindowSelectCommand>("window-select").WithDescription("Make a window the active one by ref.");
    config.AddCommand<GetCommand>("get").WithDescription("Read an element property.");
    config.AddCommand<ScreenshotCommand>("screenshot").WithDescription("Capture the window or an element to a PNG.");
    config.AddCommand<OcrCommand>("ocr").WithDescription("Read on-screen text with positions (for UI that UIA can't see).");
    config.AddCommand<ClickCommand>("click").WithDescription("Click an element by ref.");
    config.AddCommand<ClickAtCommand>("click-at").WithDescription("Click at screen coordinates.");
    config.AddCommand<DoubleClickCommand>("dblclick").WithDescription("Double-click an element by ref.");
    config.AddCommand<RightClickCommand>("rightclick").WithDescription("Right-click an element by ref.");
    config.AddCommand<HoverCommand>("hover").WithDescription("Move the mouse over an element.");
    config.AddCommand<FocusCommand>("focus").WithDescription("Give keyboard focus to an element.");
    config.AddCommand<CheckCommand>("check").WithDescription("Check a checkbox/toggle.");
    config.AddCommand<UncheckCommand>("uncheck").WithDescription("Uncheck a checkbox/toggle.");
    config.AddCommand<ToggleCommand>("toggle").WithDescription("Toggle a checkbox/toggle once.");
    config.AddCommand<SelectCommand>("select").WithDescription("Select a list item / tab / radio by ref.");
    config.AddCommand<ExpandCommand>("expand").WithDescription("Expand an expandable element.");
    config.AddCommand<CollapseCommand>("collapse").WithDescription("Collapse an expandable element.");
    config.AddCommand<TypeCommand>("type").WithDescription("Type text into the focused element.");
    config.AddCommand<FillCommand>("fill").WithDescription("Set a textbox value by ref.");
    config.AddCommand<PressCommand>("press").WithDescription("Press a key (Enter, Tab, …).");
    config.AddCommand<WaitCommand>("wait").WithDescription("Wait until an element is interactable.");
    config.AddCommand<BatchCommand>("batch").WithDescription("Run a JSON array of commands in one round-trip.");
    config.AddCommand<ListCommand>("list").WithDescription("List running sessions.");
    config.AddCommand<CloseCommand>("close").WithDescription("Close the current session.");
    config.AddCommand<CloseAllCommand>("close-all").WithDescription("Close all sessions.");
    config.AddCommand<KillAllCommand>("kill-all").WithDescription("Force-kill all daemon processes.");
    config.AddBranch("skill", skill =>
    {
        skill.SetDescription("Manage the Claude Code skill.");
        skill.AddCommand<SkillInstallCommand>("install").WithDescription("Install the skill to ~/.claude/skills.");
    });
});
return app.Run(HoistGlobals(args));

string? GetArg(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

// Allow global options before the command (playwright style: `flaui-cli -s=calc launch calc.exe`).
// Spectre binds options to a command, so move any leading globals to just after the command.
static string[] HoistGlobals(string[] input)
{
    var leading = new List<string>();
    var i = 0;
    while (i < input.Length)
    {
        var a = input[i];
        if (a is "--raw" or "--json") { leading.Add(a); i++; }
        else if (a is "-s" or "--session")
        {
            leading.Add(a);
            if (i + 1 < input.Length) { leading.Add(input[i + 1]); i += 2; } else i++;
        }
        else if (a.StartsWith("-s=", StringComparison.Ordinal) || a.StartsWith("--session=", StringComparison.Ordinal))
        {
            leading.Add(a);
            i++;
        }
        else break; // first non-global token is the command
    }

    if (leading.Count == 0 || i >= input.Length) return input;

    var result = new List<string> { input[i] };   // command
    result.AddRange(leading);
    result.AddRange(input.Skip(i + 1));            // command's own args/options
    return result.ToArray();
}
