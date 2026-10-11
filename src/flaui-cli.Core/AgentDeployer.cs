using System.Reflection;
using System.Security.Cryptography;

namespace FlauiCli.Core;

/// <summary>
/// Writes the agent assembly embedded in flaui-cli.exe to disk so a launched app can load it as a
/// startup hook. The folder is named after the content hash: an app still running with an older
/// agent keeps its file locked, and a new build lands next to it instead of failing to overwrite.
/// </summary>
internal static class AgentDeployer
{
    private const string ResourceName = "flaui-cli.Agent.dll";

    public static string Extract()
    {
        var host = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("No entry assembly to read the agent from.");
        using var stream = host.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("This flaui-cli build does not carry the in-process agent.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(bytes))[..16];
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "flaui-cli", "agent", hash);
        var path = Path.Combine(dir, ResourceName);
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, bytes);
        }
        return path;
    }
}
