using System.ComponentModel;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class ClickSettings : GlobalSettings
{
    [CommandArgument(0, "<REF>")]
    [Description("Element ref from a snapshot, e.g. w1e42.")]
    public string Ref { get; set; } = "";
}

public sealed class ClickCommand : Command<ClickSettings>
{
    public override int Execute(CommandContext context, ClickSettings settings)
    {
        var request = new DaemonRequest { Cmd = "click", Ref = settings.Ref };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}

public sealed class TypeSettings : GlobalSettings
{
    [CommandArgument(0, "<TEXT>")]
    [Description("Text to type into the focused element.")]
    public string Text { get; set; } = "";
}

public sealed class TypeCommand : Command<TypeSettings>
{
    public override int Execute(CommandContext context, TypeSettings settings)
    {
        var request = new DaemonRequest { Cmd = "type", Text = settings.Text };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}

public sealed class FillSettings : GlobalSettings
{
    [CommandArgument(0, "<REF>")]
    [Description("Textbox element ref.")]
    public string Ref { get; set; } = "";

    [CommandArgument(1, "<TEXT>")]
    [Description("Value to set.")]
    public string Text { get; set; } = "";

    [CommandOption("--submit")]
    [Description("Press Enter after filling.")]
    public bool Submit { get; set; }
}

public sealed class FillCommand : Command<FillSettings>
{
    public override int Execute(CommandContext context, FillSettings settings)
    {
        var request = new DaemonRequest { Cmd = "fill", Ref = settings.Ref, Text = settings.Text, Submit = settings.Submit };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}

public sealed class PressSettings : GlobalSettings
{
    [CommandArgument(0, "<KEY>")]
    [Description("Key name: Enter, Tab, ArrowDown, Escape, F5, …")]
    public string Key { get; set; } = "";
}

public sealed class PressCommand : Command<PressSettings>
{
    public override int Execute(CommandContext context, PressSettings settings)
    {
        var request = new DaemonRequest { Cmd = "press", Key = settings.Key };
        return Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), request));
    }
}
