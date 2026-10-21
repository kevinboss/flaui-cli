using System.Text.Json;
using FlauiCli.Commands;
using FlauiCli.Core;

namespace FlauiCli.Output;

/// <summary>Turns a <see cref="DaemonResponse"/> into console output, honoring --raw / --json.</summary>
public static class Renderer
{
    public static int Print(GlobalSettings settings, DaemonResponse response)
    {
        if (settings.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(response, Protocol.Json));
            return response.Ok ? 0 : 1;
        }

        if (!response.Ok)
        {
            Console.Error.WriteLine($"Error: {response.Error}");
            return 1;
        }

        if (settings.Raw)
        {
            Console.WriteLine(response.Value ?? response.Snapshot ?? "");
            return 0;
        }

        if (response.WindowRef is not null || response.WindowTitle is not null)
        {
            Console.WriteLine("### Window");
            Console.WriteLine($"- \"{response.WindowTitle}\" [{response.WindowRef}]");
            if (response.FocusedRef is not null)
                Console.WriteLine($"- focused: \"{response.FocusedName}\" [{response.FocusedRef}]");
        }

        if (!string.IsNullOrEmpty(response.Snapshot))
        {
            Console.WriteLine("### Snapshot");
            Console.Write(response.Snapshot);
            if (!response.Snapshot.EndsWith('\n')) Console.WriteLine();
        }
        else if (!string.IsNullOrEmpty(response.Value))
        {
            Console.WriteLine(response.Value);
        }
        else if (response.Note is not null)
        {
            Console.WriteLine(response.Note);
        }

        return 0;
    }

    /// <summary>Prints the result of a command that runs without the daemon, honoring --raw / --json.</summary>
    public static int Print(GlobalSettings settings, string? error, string? value = null, string? message = null)
    {
        if (settings.Json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok = error is null, error, value }, Protocol.Json));
            return error is null ? 0 : 1;
        }

        if (error is not null)
        {
            Console.Error.WriteLine($"Error: {error}");
            return 1;
        }

        Console.WriteLine(settings.Raw ? value : message ?? value);
        return 0;
    }
}
