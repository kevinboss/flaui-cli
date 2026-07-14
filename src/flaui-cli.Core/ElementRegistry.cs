using FlaUI.Core.AutomationElements;

namespace FlauiCli.Core;

/// <summary>
/// Hands out short, human-readable refs (<c>w1</c> for a window, <c>w1e7</c> for an element)
/// and maps them back to live <see cref="AutomationElement"/>s. Refs are stable for the life of
/// the session: the same physical element (matched by UIA RuntimeId) always keeps the same ref
/// across snapshots, so an agent can snapshot, reason, then act without the ids shifting under it.
/// All access happens on the UIA worker thread, so no locking is needed.
/// </summary>
public sealed class ElementRegistry
{
    private sealed record Known(string Reference, string? RuntimeKey, AutomationElement Element);

    private readonly Dictionary<string, Known> _byRef = new();
    private readonly Dictionary<string, Known> _byRuntime = new();
    private readonly Dictionary<string, int> _elementCounters = new();
    private int _windowCounter;

    public string GetOrAddWindow(AutomationElement window)
    {
        var key = RuntimeKey(window);
        if (key != null && _byRuntime.TryGetValue(key, out var hit)) return hit.Reference;
        var reference = "w" + (++_windowCounter);
        Store(reference, key, window);
        return reference;
    }

    public string GetOrAddElement(string windowRef, AutomationElement element)
    {
        var key = RuntimeKey(element);
        if (key != null && _byRuntime.TryGetValue(key, out var hit)) return hit.Reference;
        _elementCounters.TryGetValue(windowRef, out var n);
        _elementCounters[windowRef] = ++n;
        var reference = $"{windowRef}e{n}";
        Store(reference, key, element);
        return reference;
    }

    /// <summary>Resolve a ref to its live element, or null if the ref was never issued.</summary>
    public AutomationElement? Resolve(string reference) =>
        _byRef.TryGetValue(reference, out var known) ? known.Element : null;

    public bool Knows(string reference) => _byRef.ContainsKey(reference);

    private void Store(string reference, string? runtimeKey, AutomationElement element)
    {
        var known = new Known(reference, runtimeKey, element);
        _byRef[reference] = known;
        if (runtimeKey != null) _byRuntime[runtimeKey] = known;
    }

    private static string? RuntimeKey(AutomationElement element)
    {
        var id = Safe.Get(() => element.Properties.RuntimeId.ValueOrDefault);
        return id is { Length: > 0 } ? string.Join(".", id) : null;
    }
}
