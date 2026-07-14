using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Capture = FlaUI.Core.Capturing.Capture;

namespace FlauiCli.Core;

/// <summary>
/// One automation session: an app (launched or attached), its UIA automation, and the ref
/// registry. Every public method marshals onto the dedicated UIA worker thread, so callers
/// (the daemon dispatch loop, or an in-process test) never touch UIA off-thread.
/// </summary>
public sealed class Session : IDisposable
{
    private readonly UiaWorker _worker = new();
    private readonly ElementRegistry _registry = new();
    private UIA3Automation _automation = null!;
    private Application? _app;
    private bool _appOwned;
    private Window? _activeWindow;

    public string Name { get; }
    public DateTime LastCommandUtc { get; private set; } = DateTime.UtcNow;

    public Session(string name)
    {
        Name = name;
        _worker.Run(() => _automation = new UIA3Automation());
    }

    // ---- lifecycle ---------------------------------------------------------

    public CommandResult Launch(string executable, string arguments) => Exec(() =>
    {
        _app = Application.Launch(executable, arguments);
        _appOwned = true;
        Safe.Get<object?>(() => { _app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(10)); return null; });
        _activeWindow = FindMainWindow();
        return BuildResult($"launched {executable}");
    });

    public CommandResult AttachPid(int pid) => Exec(() =>
    {
        _app = Application.Attach(pid);
        _appOwned = false;
        _activeWindow = FindMainWindow();
        return BuildResult($"attached pid {pid}");
    });

    public CommandResult AttachProcess(string name) => Exec(() =>
    {
        _app = Application.Attach(name);
        _appOwned = false;
        _activeWindow = FindMainWindow();
        return BuildResult($"attached process {name}");
    });

    public CommandResult AttachTitle(string pattern) => Exec(() =>
    {
        var rx = new Regex(pattern, RegexOptions.IgnoreCase);
        var desktop = _automation.GetDesktop();
        var windows = Safe.Get(() => desktop.FindAllChildren()) ?? [];
        var match = windows.FirstOrDefault(w => rx.IsMatch(Safe.Str(() => w.Name)));
        if (match == null) return CommandResult.Fail($"No top-level window title matches /{pattern}/.");
        _app = Application.Attach(Safe.Get(() => match.Properties.ProcessId.ValueOrDefault));
        _appOwned = false;
        _activeWindow = match.AsWindow();
        return BuildResult($"attached title /{pattern}/");
    });

    // ---- reads -------------------------------------------------------------

    public CommandResult Snapshot(string? reference, int depth) => Exec(() =>
    {
        if (reference != null)
        {
            var el = _registry.Resolve(reference);
            if (el == null) return Unknown(reference);
            return BuildResult($"snapshot {reference}", el, depth);
        }
        return BuildResult("snapshot", null, depth);
    });

    /// <summary>Snapshot the whole desktop (all top-level windows) as a starting overview.</summary>
    public CommandResult SnapshotDesktop(int depth) => Exec(() =>
    {
        var desktop = _automation.GetDesktop();
        var snapshot = SnapshotBuilder.Build(_registry, "w0", desktop, depth);
        return new CommandResult { Ok = true, Note = "desktop", Snapshot = snapshot };
    });

    /// <summary>List the current app's top-level windows with their refs.</summary>
    public CommandResult Windows() => Exec(() =>
    {
        if (_app == null) return CommandResult.Fail("No app; launch or attach first.");
        var windows = Safe.Get(() => _app.GetAllTopLevelWindows(_automation)) ?? [];
        var activeRef = _activeWindow != null ? _registry.GetOrAddWindow(_activeWindow) : null;
        var sb = new StringBuilder();
        foreach (var w in windows)
        {
            var reference = _registry.GetOrAddWindow(w);
            var marker = reference == activeRef ? " (active)" : "";
            sb.Append("- window \"").Append(Safe.Str(() => w.Title)).Append("\" [").Append(reference).Append(']').Append(marker).Append('\n');
        }
        return new CommandResult { Ok = true, Note = "windows", Snapshot = sb.ToString() };
    });

    public CommandResult WindowSelect(string reference) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        _activeWindow = el.AsWindow();
        return BuildResult($"window-select {reference}");
    });

    public CommandResult Get(string reference, string? property) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        var value = (property ?? "").ToLowerInvariant() switch
        {
            "name" => Safe.Str(() => el.Name),
            "value" => Safe.Str(() => el.Patterns.Value.PatternOrDefault?.Value?.Value),
            "automationid" or "id" => Safe.Str(() => el.AutomationId),
            "controltype" or "role" => Roles.Of(el),
            "classname" or "class" => Safe.Str(() => el.ClassName),
            "helptext" => Safe.Str(() => el.HelpText),
            "enabled" => Safe.Bool(() => el.IsEnabled, true).ToString(),
            "offscreen" => Safe.Bool(() => el.IsOffscreen).ToString(),
            "bounds" => Bounds(el),
            "" => DumpProperties(el),
            _ => $"(unknown property '{property}')",
        };
        return new CommandResult { Ok = true, Note = $"get {reference}", Value = value };
    });

    public CommandResult Screenshot(string? reference, string? filename) => Exec(() =>
    {
        CaptureImage image;
        if (reference != null)
        {
            var el = _registry.Resolve(reference);
            if (el == null) return Unknown(reference);
            image = Capture.Element(el);
        }
        else if (_activeWindow != null) image = Capture.Element(_activeWindow);
        else image = Capture.Screen();

        var path = string.IsNullOrWhiteSpace(filename) ? DefaultShotPath() : filename;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        image.ToFile(path);
        image.Dispose();
        return new CommandResult { Ok = true, Note = "screenshot", Value = Path.GetFullPath(path) };
    });

    // ---- actions -----------------------------------------------------------

    public CommandResult Click(string r) => ActOn(r, $"click {r}", el =>
    {
        if (el.Patterns.Invoke.IsSupported) el.Patterns.Invoke.Pattern.Invoke();
        else if (el.Patterns.Toggle.IsSupported) el.Patterns.Toggle.Pattern.Toggle();
        else if (el.Patterns.SelectionItem.IsSupported) el.Patterns.SelectionItem.Pattern.Select();
        else el.Click();
    });

    public CommandResult DoubleClick(string r) => ActOn(r, $"dblclick {r}", el => el.DoubleClick());

    public CommandResult RightClick(string r) => ActOn(r, $"rightclick {r}", el => el.RightClick());

    public CommandResult Hover(string r) => ActOn(r, $"hover {r}", el =>
    {
        var b = el.BoundingRectangle;
        Mouse.MoveTo(new System.Drawing.Point(b.Left + b.Width / 2, b.Top + b.Height / 2));
    });

    public CommandResult Focus(string r) => ActOn(r, $"focus {r}", el => el.Focus());

    public CommandResult Toggle(string r) => ActOn(r, $"toggle {r}", el => el.Patterns.Toggle.Pattern.Toggle());

    public CommandResult Check(string r) => ActOn(r, $"check {r}", el => SetToggle(el, ToggleState.On));

    public CommandResult Uncheck(string r) => ActOn(r, $"uncheck {r}", el => SetToggle(el, ToggleState.Off));

    public CommandResult Select(string r) => ActOn(r, $"select {r}", el =>
    {
        var sip = el.Patterns.SelectionItem.PatternOrDefault;
        if (sip != null) sip.Select();
        else el.Click();
    });

    public CommandResult Expand(string r) => ActOn(r, $"expand {r}", el => el.Patterns.ExpandCollapse.Pattern.Expand());

    public CommandResult Collapse(string r) => ActOn(r, $"collapse {r}", el => el.Patterns.ExpandCollapse.Pattern.Collapse());

    public CommandResult Type(string text) => Exec(() =>
    {
        Keyboard.Type(text);
        return BuildResult($"type \"{text}\"");
    });

    public CommandResult Fill(string r, string text, bool submit) => ActOn(r, $"fill {r}", el =>
    {
        var value = el.Patterns.Value.PatternOrDefault;
        if (value != null && !value.IsReadOnly.ValueOrDefault) value.SetValue(text);
        else { el.Focus(); Keyboard.Type(text); }
        if (submit) Keyboard.Type(VirtualKeyShort.RETURN);
    });

    public CommandResult Press(string keyName) => Exec(() =>
    {
        if (!Keys.TryResolve(keyName, out var vk))
            return CommandResult.Fail($"Unknown key '{keyName}'. Known keys: {Keys.KnownNames}");
        Keyboard.Type(vk);
        return BuildResult($"press {keyName}");
    });

    public CommandResult Wait(string reference, int timeoutMs) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (Safe.Bool(() => el.IsEnabled, true) && !Safe.Bool(() => el.IsOffscreen))
                return BuildResult($"wait {reference} ready");
            Thread.Sleep(100);
        }
        return CommandResult.Fail($"'{reference}' not interactable within {timeoutMs} ms.");
    });

    // ---- internals (all run on the worker thread) --------------------------

    private CommandResult ActOn(string reference, string note, Action<AutomationElement> action) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        WaitInteractable(el);
        try { action(el); }
        catch (Exception ex) { return Stale(reference, ex); }
        return BuildResult(note);
    });

    private CommandResult Exec(Func<CommandResult> body)
    {
        LastCommandUtc = DateTime.UtcNow;
        return _worker.Run(() =>
        {
            try { return body(); }
            catch (Exception ex) { return CommandResult.Fail(ex.Message); }
        });
    }

    private CommandResult BuildResult(string? note = null, AutomationElement? snapshotRoot = null, int depth = SnapshotBuilder.DefaultDepth)
    {
        if (_activeWindow == null)
            return new CommandResult { Ok = true, Note = note, Snapshot = "(no active window; launch or attach first)" };

        var windowRef = _registry.GetOrAddWindow(_activeWindow);
        var title = Safe.Str(() => _activeWindow.Title);
        var root = snapshotRoot ?? _activeWindow;
        var snapshot = SnapshotBuilder.Build(_registry, windowRef, root, depth);

        string? focusedRef = null, focusedName = null;
        var focused = Safe.Get<AutomationElement?>(() => _automation.FocusedElement());
        if (focused != null)
        {
            focusedRef = _registry.GetOrAddElement(windowRef, focused);
            focusedName = Safe.Str(() => focused.Name);
        }

        return new CommandResult
        {
            Ok = true,
            Note = note,
            WindowRef = windowRef,
            WindowTitle = title,
            FocusedRef = focusedRef,
            FocusedName = focusedName,
            Snapshot = snapshot,
        };
    }

    private Window? FindMainWindow()
    {
        Window? window = null;
        for (var i = 0; i < 20 && window == null; i++)
        {
            window = Safe.Get(() => _app!.GetMainWindow(_automation));
            if (window == null)
            {
                var all = Safe.Get(() => _app!.GetAllTopLevelWindows(_automation)) ?? [];
                if (all.Length > 0) window = all[0];
            }
            if (window == null) Thread.Sleep(150);
        }
        return window;
    }

    private static void WaitInteractable(AutomationElement el)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (Safe.Bool(() => el.IsEnabled, true) && !Safe.Bool(() => el.IsOffscreen)) return;
            Thread.Sleep(100);
        }
    }

    private static void SetToggle(AutomationElement el, ToggleState desired)
    {
        var toggle = el.Patterns.Toggle.PatternOrDefault;
        if (toggle == null)
        {
            if (desired == ToggleState.On) el.Click();
            return;
        }
        for (var i = 0; i < 3 && toggle.ToggleState.Value != desired; i++)
            toggle.Toggle();
    }

    private static string Bounds(AutomationElement el)
    {
        var b = Safe.Get(() => el.BoundingRectangle);
        return $"{b.X},{b.Y} {b.Width}x{b.Height}";
    }

    private static string DumpProperties(AutomationElement el)
    {
        var sb = new StringBuilder();
        sb.Append("role=").Append(Roles.Of(el)).Append('\n');
        sb.Append("name=").Append(Safe.Str(() => el.Name)).Append('\n');
        sb.Append("automationId=").Append(Safe.Str(() => el.AutomationId)).Append('\n');
        sb.Append("className=").Append(Safe.Str(() => el.ClassName)).Append('\n');
        sb.Append("helpText=").Append(Safe.Str(() => el.HelpText)).Append('\n');
        sb.Append("value=").Append(Safe.Str(() => el.Patterns.Value.PatternOrDefault?.Value?.Value)).Append('\n');
        sb.Append("enabled=").Append(Safe.Bool(() => el.IsEnabled, true)).Append('\n');
        sb.Append("offscreen=").Append(Safe.Bool(() => el.IsOffscreen)).Append('\n');
        sb.Append("bounds=").Append(Bounds(el));
        return sb.ToString();
    }

    private static string DefaultShotPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "flaui-cli", "shots");
        return Path.Combine(dir, $"shot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
    }

    private static CommandResult Unknown(string reference) =>
        CommandResult.Fail($"Unknown ref '{reference}'. Run 'snapshot' to refresh element refs.");

    private static CommandResult Stale(string reference, Exception ex)
    {
        var comish = ex is COMException || ex.GetType().Name.Contains("ElementNotAvailable", StringComparison.Ordinal);
        return comish
            ? CommandResult.Fail($"'{reference}' looks stale ({ex.GetType().Name}); run 'snapshot' and retry.")
            : CommandResult.Fail($"'{reference}': {ex.Message}");
    }

    public void Dispose()
    {
        _worker.Run(() =>
        {
            if (_appOwned) Safe.Get<object?>(() => { _app?.Close(); return null; });
            Safe.Get<object?>(() => { _automation.Dispose(); return null; });
        });
        _worker.Dispose();
    }
}
