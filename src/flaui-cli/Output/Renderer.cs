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
        else if (response.Note is not null)
        {
            Console.WriteLine(response.Note);
        }

        return 0;
    }
}
