using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FlauiCli.Core;

/// <summary>
/// Maps UIA <see cref="ControlType"/> to short, Playwright-style semantic role names and
/// classifies roles for the snapshot's include/skip policy.
/// </summary>
public static class Roles
{
    public static string Of(AutomationElement el)
    {
        ControlType ct;
        try { ct = el.ControlType; }
        catch { return "element"; }
        return ct switch
        {
            ControlType.Button => "button",
            ControlType.SplitButton => "button",
            ControlType.Calendar => "calendar",
            ControlType.CheckBox => "checkbox",
            ControlType.ComboBox => "combobox",
            ControlType.Edit => "textbox",
            ControlType.Hyperlink => "link",
            ControlType.Image => "image",
            ControlType.ListItem => "listitem",
            ControlType.List => "list",
            ControlType.Menu => "menu",
            ControlType.MenuBar => "menubar",
            ControlType.MenuItem => "menuitem",
            ControlType.ProgressBar => "progressbar",
            ControlType.RadioButton => "radio",
            ControlType.ScrollBar => "scrollbar",
            ControlType.Slider => "slider",
            ControlType.Spinner => "spinner",
            ControlType.StatusBar => "statusbar",
            ControlType.Tab => "tablist",
            ControlType.TabItem => "tab",
            ControlType.Text => "text",
            ControlType.ToolBar => "toolbar",
            ControlType.ToolTip => "tooltip",
            ControlType.Tree => "tree",
            ControlType.TreeItem => "treeitem",
            ControlType.Group => "group",
            ControlType.Thumb => "thumb",
            ControlType.DataGrid => "grid",
            ControlType.DataItem => "row",
            ControlType.Document => "document",
            ControlType.Header => "header",
            ControlType.HeaderItem => "headeritem",
            ControlType.Table => "table",
            ControlType.TitleBar => "titlebar",
            ControlType.Separator => "separator",
            ControlType.Pane => "pane",
            ControlType.Window => "window",
            ControlType.Custom => "custom",
            _ => "element",
        };
    }

    /// <summary>Decorative chrome that adds noise: skipped from output and not descended into.</summary>
    public static bool IsDecorative(string role) =>
        role is "scrollbar" or "separator" or "thumb" or "titlebar" or "tooltip";

    /// <summary>Interactive or structural roles worth showing even when they have no name.</summary>
    public static bool IsAlwaysShown(string role) =>
        role is "button" or "textbox" or "checkbox" or "radio" or "combobox" or "listitem"
            or "menuitem" or "tab" or "treeitem" or "link" or "slider" or "spinner"
            or "window" or "group" or "list" or "tree" or "tablist" or "menu" or "menubar"
            or "toolbar" or "grid" or "table" or "document" or "row";

    /// <summary>Roles whose current value is useful to show inline (e.g. a textbox's text).</summary>
    public static bool CarriesValue(string role) =>
        role is "textbox" or "document" or "combobox" or "spinner" or "slider";
}
