using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FlauiCli.Core;

/// <summary>
/// Renders a live element subtree as an indented, Playwright-style outline:
/// <code>- button "Seven" [ref=w1e42] [disabled]</code>
/// Bounded by both depth and a node budget so a sprawling app can't blow the output up.
/// </summary>
public static class SnapshotBuilder
{
    public const int DefaultDepth = 20;
    private const int MaxNodes = 400;
    private const int MaxNameLength = 100;

    public static string Build(ElementRegistry registry, string windowRef, AutomationElement root, int maxDepth)
    {
        var sb = new StringBuilder();
        var nodes = 0;
        var truncated = false;

        void Walk(AutomationElement el, int recursion, int indent)
        {
            if (truncated) return;
            var role = Roles.Of(el);
            if (recursion > 0 && Roles.IsDecorative(role)) return;

            var name = Safe.Str(() => el.Name);
            var include = recursion == 0 || Roles.IsAlwaysShown(role) || name.Length > 0;
            var childIndent = indent;

            if (include)
            {
                if (nodes >= MaxNodes) { sb.Append(Pad(indent)).Append("- [… truncated]\n"); truncated = true; return; }
                nodes++;

                var reference = role == "window" ? registry.GetOrAddWindow(el) : registry.GetOrAddElement(windowRef, el);
                sb.Append(Pad(indent)).Append("- ").Append(role);
                if (name.Length > 0) sb.Append(" \"").Append(Escape(Truncate(name))).Append('"');
                sb.Append(" [ref=").Append(reference).Append(']');
                AppendStates(sb, el, role);
                AppendValue(sb, el, role, name);
                sb.Append('\n');
                childIndent = indent + 1;
            }

            if (recursion >= maxDepth) return;
            AutomationElement[] children;
            try { children = el.FindAllChildren(); }
            catch { return; }
            foreach (var child in children.Concat(SelectedOutsideChildren(el, children)))
            {
                Walk(child, recursion + 1, childIndent);
                if (truncated) return;
            }
        }

        Walk(root, 0, 0);
        return sb.ToString();
    }

    /// <summary>
    /// Selected items a container reports through the Selection pattern but leaves out of its
    /// children. Some WPF controls keep a stale child list (items added after the first query never
    /// appear), while the selection is resolved fresh, so this is the only route to that content.
    /// </summary>
    private static IEnumerable<AutomationElement> SelectedOutsideChildren(AutomationElement el, AutomationElement[] children)
    {
        var selected = Safe.Get(() => el.Patterns.Selection.PatternOrDefault?.Selection?.Value);
        if (selected is not { Length: > 0 }) return [];

        var known = children.Select(ElementRegistry.RuntimeKey).OfType<string>().ToHashSet();
        return selected.Where(s => ElementRegistry.RuntimeKey(s) is not { } key || !known.Contains(key));
    }

    private static void AppendStates(StringBuilder sb, AutomationElement el, string role)
    {
        if (!Safe.Bool(() => el.IsEnabled, true)) sb.Append(" [disabled]");
        if (Safe.Bool(() => el.IsOffscreen)) sb.Append(" [offscreen]");

        var toggle = Safe.Get(() => el.Patterns.Toggle.PatternOrDefault?.ToggleState?.Value);
        if (toggle == ToggleState.On) sb.Append(" [checked]");
        else if (toggle == ToggleState.Indeterminate) sb.Append(" [indeterminate]");

        if (Safe.Get(() => el.Patterns.SelectionItem.PatternOrDefault?.IsSelected?.Value) == true)
            sb.Append(" [selected]");

        var expand = Safe.Get(() => el.Patterns.ExpandCollapse.PatternOrDefault?.ExpandCollapseState?.Value);
        if (expand == ExpandCollapseState.Expanded) sb.Append(" [expanded]");
        else if (expand == ExpandCollapseState.Collapsed) sb.Append(" [collapsed]");

        if (Safe.Get(() => el.Patterns.Value.PatternOrDefault?.IsReadOnly?.Value) == true)
            sb.Append(" [readonly]");
    }

    private static void AppendValue(StringBuilder sb, AutomationElement el, string role, string name)
    {
        if (!Roles.CarriesValue(role)) return;
        var value = Safe.Str(() => el.Patterns.Value.PatternOrDefault?.Value?.Value);
        if (value.Length == 0 || value == name) return;
        sb.Append(" value=\"").Append(Escape(Truncate(value))).Append('"');
    }

    private static string Pad(int indent) => new(' ', indent * 2);

    private static string Truncate(string s) => s.Length <= MaxNameLength ? s : s[..MaxNameLength] + "…";

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
}
