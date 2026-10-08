using System.ComponentModel;
using FlauiCli.Core;
using FlauiCli.Ipc;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class OcrSettings : GlobalSettings
{
    [CommandArgument(0, "[REF]")]
    [Description("Read only this element's area instead of the whole active window.")]
    public string? Ref { get; set; }

    [CommandOption("--words")]
    [Description("Also list each word with its own position.")]
    public bool Words { get; set; }

    [CommandOption("--lang <TAG>")]
    [Description("OCR language, e.g. de-CH. Defaults to the user profile languages.")]
    public string? Lang { get; set; }
}

public sealed class OcrCommand : Command<OcrSettings>
{
    public override int Execute(CommandContext context, OcrSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "ocr", Ref = settings.Ref, Words = settings.Words, Lang = settings.Lang }));
}

public sealed class ClickAtSettings : GlobalSettings
{
    [CommandArgument(0, "<X>")]
    [Description("Screen x coordinate, e.g. from an 'ocr' [at=x,y].")]
    public int X { get; set; }

    [CommandArgument(1, "<Y>")]
    [Description("Screen y coordinate.")]
    public int Y { get; set; }

    [CommandOption("--double")]
    [Description("Double-click instead of a single click.")]
    public bool Double { get; set; }

    [CommandOption("--right")]
    [Description("Use the right mouse button.")]
    public bool Right { get; set; }
}

public sealed class ClickAtCommand : Command<ClickAtSettings>
{
    public override int Execute(CommandContext context, ClickAtSettings settings, CancellationToken cancellationToken) =>
        Renderer.Print(settings, DaemonClient.Send(settings.ResolveSession(), new DaemonRequest { Cmd = "click-at", X = settings.X, Y = settings.Y, Double = settings.Double, Right = settings.Right }));
}
