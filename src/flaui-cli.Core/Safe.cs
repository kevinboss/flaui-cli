namespace FlauiCli.Core;

/// <summary>
/// Reading a UIA property or pattern can throw (element gone, provider busy, COM hiccup).
/// These helpers swallow those failures and return a default so one dead element never aborts
/// a whole snapshot. Must be called on the UIA worker thread.
/// </summary>
public static class Safe
{
    public static T? Get<T>(Func<T> read, T? fallback = default)
    {
        try { return read(); }
        catch { return fallback; }
    }

    public static string Str(Func<string?> read)
    {
        try { return read() ?? ""; }
        catch { return ""; }
    }

    public static bool Bool(Func<bool> read, bool fallback = false)
    {
        try { return read(); }
        catch { return fallback; }
    }
}
