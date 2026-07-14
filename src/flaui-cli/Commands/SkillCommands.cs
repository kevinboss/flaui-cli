using System.Reflection;
using Spectre.Console.Cli;

namespace FlauiCli.Commands;

/// <summary>Writes the embedded SKILL.md into the user's Claude Code skills directory.</summary>
public sealed class SkillInstallCommand : Command<GlobalSettings>
{
    public override int Execute(CommandContext context, GlobalSettings settings)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SKILL.md");
        if (stream is null)
        {
            Console.Error.WriteLine("Error: embedded SKILL.md not found in this build.");
            return 1;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(home, ".claude", "skills", "flaui-cli");
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, "SKILL.md");

        using (var file = File.Create(target))
            stream.CopyTo(file);

        Console.WriteLine($"Installed skill to {target}");
        return 0;
    }
}
