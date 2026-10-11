/// <summary>
/// Entry point the .NET runtime calls before the app's Main when the process starts with
/// DOTNET_STARTUP_HOOKS pointing at this assembly. The type must be named StartupHook and live in
/// the global namespace.
/// </summary>
internal static class StartupHook
{
    public static void Initialize() => FlauiCli.Agent.AgentServer.Start();
}
