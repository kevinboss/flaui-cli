using System.Text.RegularExpressions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

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

    // ---- actions -----------------------------------------------------------

    public CommandResult Click(string reference) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        WaitInteractable(el);
        try
        {
            if (el.Patterns.Invoke.IsSupported) el.Patterns.Invoke.Pattern.Invoke();
            else if (el.Patterns.Toggle.IsSupported) el.Patterns.Toggle.Pattern.Toggle();
            else if (el.Patterns.SelectionItem.IsSupported) el.Patterns.SelectionItem.Pattern.Select();
            else el.Click();
        }
        catch (Exception ex) { return Stale(reference, ex); }
        return BuildResult($"click {reference}");
    });

    public CommandResult Type(string text) => Exec(() =>
    {
        Keyboard.Type(text);
        return BuildResult($"type \"{text}\"");
    });

    public CommandResult Fill(string reference, string text, bool submit) => Exec(() =>
    {
        var el = _registry.Resolve(reference);
        if (el == null) return Unknown(reference);
        WaitInteractable(el);
        try
        {
            var value = el.Patterns.Value.PatternOrDefault;
            if (value != null && !value.IsReadOnly.ValueOrDefault)
                value.SetValue(text);
            else
            {
                el.Focus();
                Keyboard.Type(text);
            }
            if (submit) Keyboard.Type(VirtualKeyShort.RETURN);
        }
        catch (Exception ex) { return Stale(reference, ex); }
        return BuildResult($"fill {reference}");
    });

    public CommandResult Press(string keyName) => Exec(() =>
    {
        if (!Keys.TryResolve(keyName, out var vk))
            return CommandResult.Fail($"Unknown key '{keyName}'. Known keys: {Keys.KnownNames}");
        Keyboard.Type(vk);
        return BuildResult($"press {keyName}");
    });

    // ---- internals (all run on the worker thread) --------------------------

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

    private static CommandResult Unknown(string reference) =>
        CommandResult.Fail($"Unknown ref '{reference}'. Run 'snapshot' to refresh element refs.");

    private static CommandResult Stale(string reference, Exception ex) =>
        CommandResult.Fail($"Action on '{reference}' failed ({ex.GetType().Name}: {ex.Message}). " +
                           "The element may be stale; run 'snapshot' and retry.");

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
