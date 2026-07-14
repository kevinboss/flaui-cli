using System.Collections.Concurrent;

namespace FlauiCli.Core;

/// <summary>
/// A single long-lived MTA thread that owns all UI Automation work. Microsoft's UIA guidance
/// says a desktop-wide client must make every call from one dedicated MTA thread that owns no
/// windows; element references are apartment-bound, so anything that creates or touches an
/// <c>AutomationElement</c> must run here. Callers marshal work in via <see cref="Run{T}"/>.
/// </summary>
public sealed class UiaWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public UiaWorker()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "uia-worker" };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }

    private void Loop()
    {
        foreach (var action in _queue.GetConsumingEnumerable())
            action();
    }

    /// <summary>Run <paramref name="func"/> on the worker thread and block until it returns.</summary>
    public T Run<T>(Func<T> func)
    {
        // Never call Run from inside a queued action: the worker is single-threaded and would deadlock.
        var box = new ResultBox<T>();
        _queue.Add(() =>
        {
            try { box.Value = func(); }
            catch (Exception ex) { box.Error = ex; }
            finally { box.Done.Set(); }
        });
        box.Done.Wait();
        if (box.Error != null) throw box.Error;
        return box.Value!;
    }

    public void Run(Action action) => Run<object?>(() => { action(); return null; });

    public void Dispose() => _queue.CompleteAdding();

    private sealed class ResultBox<T>
    {
        public T? Value;
        public Exception? Error;
        public readonly ManualResetEventSlim Done = new(false);
    }
}
