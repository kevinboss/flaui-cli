using System.ComponentModel;
using System.Reflection;
using FlauiCli.Output;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

public sealed class SkillInstallSettings : GlobalSettings
{
    [CommandOption("-f|--force")]
    [Description("Overwrite an existing SKILL.md.")]
    public bool Force { get; set; }
}

/// <summary>Writes the embedded SKILL.md into the user's Claude Code skills directory.</summary>
public sealed class SkillInstallCommand : Command<SkillInstallSettings>
{
    public override int Execute(CommandContext context, SkillInstallSettings settings, CancellationToken cancellationToken)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SKILL.md");
        if (stream is null)
            return Renderer.Print(settings, error: "embedded SKILL.md not found in this build.");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(home, ".claude", "skills", "flaui-cli");
        var target = Path.Combine(dir, "SKILL.md");

        if (File.Exists(target) && !settings.Force)
            return Renderer.Print(settings, error: $"{target} already exists. Use --force to overwrite.");

        Directory.CreateDirectory(dir);
        using (var file = File.Create(target))
            stream.CopyTo(file);

        return Renderer.Print(settings, error: null, value: target, message: $"Installed skill to {target}");
    }
}
