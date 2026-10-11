using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FlauiCli.Agent;

/// <summary>
/// Reads and drives the app's WPF tree from inside the process. Actions go through an automation
/// peer created directly for the target element, so they work without real input, focus or a
/// visible window, and do not depend on the app's automation tree being connected.
/// Must run on the UI thread.
/// </summary>
internal static class WpfAutomation
{
    private const int MaxNodes = 800;
    private const int MaxLabelLength = 100;

    private static readonly ConditionalWeakTable<DependencyObject, string> RefOf = new();
    private static readonly Dictionary<string, WeakReference<DependencyObject>> ByRef = new();
    private static int _nextRef;

    public static AgentResponse Run(AgentRequest r) => r.Cmd switch
    {
        "tree" => Tree(r.Ref, r.Depth ?? int.MaxValue),
        "get" => Get(r.Ref, r.Text),
        "ready" => Ready(r.Ref),
        "click" => Act(r.Ref, Click),
        "dblclick" => Act(r.Ref, DoubleClick),
        "fill" => Act(r.Ref, el => Fill(el, r.Text ?? "")),
        "select" => Act(r.Ref, Select),
        "check" => Act(r.Ref, el => SetToggle(el, ToggleState.On)),
        "uncheck" => Act(r.Ref, el => SetToggle(el, ToggleState.Off)),
        "toggle" => Act(r.Ref, el => Pattern<IToggleProvider>(el, PatternInterface.Toggle).Toggle()),
        "expand" => Act(r.Ref, el => Pattern<IExpandCollapseProvider>(el, PatternInterface.ExpandCollapse).Expand()),
        "collapse" => Act(r.Ref, el => Pattern<IExpandCollapseProvider>(el, PatternInterface.ExpandCollapse).Collapse()),
        "focus" => Act(r.Ref, Focus),
        "key" => Act(r.Ref, el => PressKey(el, r.Key ?? "")),
        _ => AgentResponse.Fail($"Unknown agent command '{r.Cmd}'."),
    };

    // ---- tree ----------------------------------------------------------------

    private static AgentResponse Tree(string? reference, int maxDepth)
    {
        var writer = new TreeWriter(maxDepth);
        if (reference != null)
        {
            if (!TryResolve(reference, out var el, out var error)) return AgentResponse.Fail(error);
            writer.Walk(el, 0);
        }
        else
        {
            foreach (var root in Roots())
            {
                if (root is Window) writer.Walk(root, 0);
                else writer.WalkPopup(root);
            }
        }
        return new AgentResponse { Snapshot = writer.ToString() };
    }

    private static IEnumerable<UIElement> Roots() =>
        PresentationSource.CurrentSources.OfType<PresentationSource>()
            .Select(s => s.RootVisual)
            .OfType<UIElement>()
            .Where(v => v.IsVisible)
            .OrderBy(v => v is Window ? 0 : 1);

    private sealed class TreeWriter(int maxDepth)
    {
        private readonly StringBuilder _sb = new();
        private int _nodes;
        private bool _truncated;

        public void WalkPopup(UIElement root)
        {
            if (!Line(0, "popup", "", root)) return;
            WalkChildren(root, 1);
        }

        public void Walk(DependencyObject d, int indent)
        {
            if (_truncated || d is UIElement { IsVisible: false }) return;
            var (role, label, leaf) = Describe(d);
            if (role != null)
            {
                if (indent > maxDepth) return;
                if (!Line(indent, role, label, d)) return;
                if (leaf) return;
                indent++;
            }
            WalkChildren(d, indent);
        }

        private void WalkChildren(DependencyObject d, int indent)
        {
            if (d is not Visual and not System.Windows.Media.Media3D.Visual3D) return;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d) && !_truncated; i++)
                Walk(VisualTreeHelper.GetChild(d, i), indent);
        }

        private bool Line(int indent, string role, string label, DependencyObject d)
        {
            if (_nodes >= MaxNodes)
            {
                _sb.Append(' ', indent * 2).Append("- [… truncated]\n");
                _truncated = true;
                return false;
            }
            _nodes++;
            _sb.Append(' ', indent * 2).Append("- ").Append(role);
            if (label.Length > 0) _sb.Append(" \"").Append(Escape(Truncate(label))).Append('"');
            _sb.Append(" [ref=").Append(RefFor(d)).Append(']');
            AppendStates(_sb, d);
            _sb.Append('\n');
            return true;
        }

        public override string ToString() => _sb.Length > 0 ? _sb.ToString() : "(no visible WPF windows)\n";
    }

    private static (string? Role, string Label, bool Leaf) Describe(DependencyObject d) => d switch
    {
        Window w => ("window", w.Title ?? "", false),
        CheckBox => ("checkbox", Label(d), IsLeaf(d)),
        RadioButton => ("radio", Label(d), IsLeaf(d)),
        ToggleButton => ("togglebutton", Label(d), IsLeaf(d)),
        ButtonBase => ("button", Label(d), IsLeaf(d)),
        TabItem => ("tab", Label(d), IsLeaf(d)),
        ComboBoxItem => ("option", Label(d), IsLeaf(d)),
        ListBoxItem => ("listitem", Label(d), IsLeaf(d)),
        DataGridRow => ("row", Label(d), IsLeaf(d)),
        TreeViewItem t => ("treeitem", HeaderText(t.Header), false),
        MenuItem m => ("menuitem", HeaderText(m.Header), false),
        Expander e => ("group", HeaderText(e.Header), false),
        TextBox => ("textbox", "", true),
        PasswordBox => ("password", "", true),
        ComboBox => ("combobox", "", true),
        DatePicker => ("datepicker", "", true),
        TabControl => ("tablist", "", false),
        DataGrid => ("table", "", false),
        TreeView => ("tree", "", false),
        Selector => ("list", "", false),
        TextBlock { Text.Length: > 0 } t => ("text", t.Text, true),
        Label { Content: string s } when s.Length > 0 => ("text", s, true),
        _ => (null, "", false),
    };

    private static void AppendStates(StringBuilder sb, DependencyObject d)
    {
        if (d is FrameworkElement { Name.Length: > 0 } fe) sb.Append(" [name=").Append(fe.Name).Append(']');
        if (d is UIElement { IsEnabled: false }) sb.Append(" [disabled]");
        if (d is ToggleButton toggle)
        {
            if (toggle.IsChecked == true) sb.Append(" [checked]");
            else if (toggle.IsChecked == null) sb.Append(" [indeterminate]");
        }
        if (d is ListBoxItem or TabItem && Selector.GetIsSelected(d)) sb.Append(" [selected]");
        if (d is Expander expander) sb.Append(expander.IsExpanded ? " [expanded]" : " [collapsed]");
        if (d is TreeViewItem item && item.HasItems) sb.Append(item.IsExpanded ? " [expanded]" : " [collapsed]");
        if (d is TextBox { IsReadOnly: true }) sb.Append(" [readonly]");

        var value = Value(d);
        if (!string.IsNullOrEmpty(value)) sb.Append(" value=\"").Append(Escape(Truncate(value))).Append('"');
    }

    private static string? Value(DependencyObject d) => d switch
    {
        TextBox t => t.Text,
        ComboBox c => c.Text,
        DatePicker p => p.Text,
        _ => null,
    };

    /// <summary>
    /// The element's own text: its automation name, string content, or the visible text below it,
    /// leaving out text that belongs to nested interactive elements (a tile button holding a list
    /// of link buttons is labelled by its header, not by every link).
    /// </summary>
    private static string Label(DependencyObject d)
    {
        var name = AutomationProperties.GetName(d);
        if (!string.IsNullOrWhiteSpace(name)) return name;
        if (d is ContentControl { Content: string s }) return s;
        return string.Join(" ", OwnTexts(d));
    }

    private static IEnumerable<string> OwnTexts(DependencyObject root)
    {
        if (root is not Visual) yield break;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is UIElement { IsVisible: false } || IsInteractive(child)) continue;
            if (child is TextBlock { Text: var text } && !string.IsNullOrWhiteSpace(text)) yield return text;
            foreach (var t in OwnTexts(child)) yield return t;
        }
    }

    private static bool IsLeaf(DependencyObject d) => !VisibleDescendants(d).Any(IsInteractive);

    private static bool IsInteractive(DependencyObject d) =>
        d is ButtonBase or TextBoxBase or PasswordBox or Selector or ListBoxItem or TabItem or DataGridRow or DatePicker;

    private static string HeaderText(object? header) => header switch
    {
        string s => s,
        DependencyObject d => Label(d),
        _ => "",
    };

    private static IEnumerable<DependencyObject> VisibleDescendants(DependencyObject root)
    {
        if (root is not Visual) yield break;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is UIElement { IsVisible: false }) continue;
            yield return child;
            foreach (var d in VisibleDescendants(child)) yield return d;
        }
    }

    // ---- reads -----------------------------------------------------------------

    private static AgentResponse Get(string? reference, string? property)
    {
        if (!TryResolve(reference, out var d, out var error)) return AgentResponse.Fail(error);
        var el = d as FrameworkElement;
        var (role, label, _) = Describe(d);
        var props = new (string Key, string Value)[]
        {
            ("role", role ?? ""),
            ("label", role == null ? Label(d) : label),
            ("name", el?.Name ?? ""),
            ("type", d.GetType().FullName ?? ""),
            ("value", Value(d) ?? ""),
            ("enabled", (d is UIElement { IsEnabled: true }).ToString()),
            ("visible", (d is UIElement { IsVisible: true }).ToString()),
        };
        if (string.IsNullOrEmpty(property)) return new AgentResponse { Value = string.Join("\n", props.Select(p => $"{p.Key}={p.Value}")) };

        var match = props.FirstOrDefault(p => p.Key.Equals(property, StringComparison.OrdinalIgnoreCase));
        return match.Key is null ? AgentResponse.Fail($"Unknown property '{property}'.") : new AgentResponse { Value = match.Value };
    }

    private static AgentResponse Ready(string? reference)
    {
        if (!TryResolve(reference, out var d, out var error)) return AgentResponse.Fail(error);
        return new AgentResponse { Value = (d is UIElement { IsEnabled: true, IsVisible: true }).ToString() };
    }

    // ---- actions ---------------------------------------------------------------

    private static AgentResponse Act(string? reference, Action<UIElement> action)
    {
        if (!TryResolve(reference, out var d, out var error)) return AgentResponse.Fail(error);
        if (d is not UIElement el) return AgentResponse.Fail($"'{reference}' is not an interactive element.");
        action(el);
        return new AgentResponse { Value = "done" };
    }

    private static void Click(UIElement el)
    {
        var peer = UIElementAutomationPeer.CreatePeerForElement(el);
        if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke) { invoke.Invoke(); return; }
        if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle) { toggle.Toggle(); return; }
        if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider item) { item.Select(); return; }

        RaiseMouse(el, UIElement.PreviewMouseLeftButtonDownEvent);
        RaiseMouse(el, UIElement.MouseLeftButtonDownEvent);
        RaiseMouse(el, UIElement.PreviewMouseLeftButtonUpEvent);
        RaiseMouse(el, UIElement.MouseLeftButtonUpEvent);
    }

    private static void DoubleClick(UIElement el)
    {
        if (el is not Control) throw new InvalidOperationException($"{el.GetType().Name} has no double-click event; only controls do.");
        RaiseMouse(el, Control.PreviewMouseDoubleClickEvent);
        RaiseMouse(el, Control.MouseDoubleClickEvent);
    }

    private static void RaiseMouse(UIElement el, RoutedEvent routedEvent) =>
        el.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routedEvent });

    private static void Fill(UIElement el, string text)
    {
        switch (el)
        {
            case PasswordBox password:
                password.Password = text;
                return;
            case DatePicker picker:
                picker.Text = text;
                return;
            default:
                Pattern<IValueProvider>(el, PatternInterface.Value).SetValue(text);
                return;
        }
    }

    private static void Select(UIElement el)
    {
        if (UIElementAutomationPeer.CreatePeerForElement(el)?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider item) item.Select();
        else Selector.SetIsSelected(el, true);
    }

    private static void SetToggle(UIElement el, ToggleState desired)
    {
        var toggle = Pattern<IToggleProvider>(el, PatternInterface.Toggle);
        for (var i = 0; i < 3 && toggle.ToggleState != desired; i++) toggle.Toggle();
    }

    private static void Focus(UIElement el)
    {
        if (!el.Focus()) throw new InvalidOperationException($"{el.GetType().Name} did not accept focus.");
    }

    private static void PressKey(UIElement el, string keyName)
    {
        var name = keyName.StartsWith("Arrow", StringComparison.OrdinalIgnoreCase) ? keyName[5..] : keyName;
        if (!Enum.TryParse<Key>(name, true, out var key)) throw new InvalidOperationException($"Unknown key '{keyName}'.");
        var source = PresentationSource.FromVisual(el) ?? throw new InvalidOperationException("Element is not attached to a window.");

        foreach (var routedEvent in new[] { Keyboard.PreviewKeyDownEvent, Keyboard.KeyDownEvent, Keyboard.PreviewKeyUpEvent, Keyboard.KeyUpEvent })
            el.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = routedEvent });
    }

    private static T Pattern<T>(UIElement el, PatternInterface pattern) where T : class =>
        UIElementAutomationPeer.CreatePeerForElement(el)?.GetPattern(pattern) as T
        ?? throw new InvalidOperationException($"{el.GetType().Name} does not support {pattern}.");

    // ---- refs ------------------------------------------------------------------

    private static string RefFor(DependencyObject d)
    {
        if (RefOf.TryGetValue(d, out var existing)) return existing;
        var reference = "p" + ++_nextRef;
        RefOf.Add(d, reference);
        ByRef[reference] = new WeakReference<DependencyObject>(d);
        return reference;
    }

    private static bool TryResolve(string? reference, out DependencyObject element, out string error)
    {
        element = null!;
        if (reference is null || !ByRef.TryGetValue(reference, out var weak) || !weak.TryGetTarget(out var target))
        {
            error = $"Unknown ref '{reference}'. Run 'snapshot' to refresh element refs.";
            return false;
        }
        if (target is Visual visual && PresentationSource.FromVisual(visual) is null)
        {
            error = $"'{reference}' is no longer on screen; run 'snapshot' and retry.";
            return false;
        }
        element = target;
        error = "";
        return true;
    }

    private static string Truncate(string s) => s.Length <= MaxLabelLength ? s : s[..MaxLabelLength] + "…";

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
}
