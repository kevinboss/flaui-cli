using System.ComponentModel;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

/// <summary>Shared settings for verbs that take a single element ref.</summary>
public class RefSettings : GlobalSettings
{
    [CommandArgument(0, "<REF>")]
    [Description("Element ref from a snapshot, e.g. w1e42.")]
    public string Ref { get; set; } = "";
}
