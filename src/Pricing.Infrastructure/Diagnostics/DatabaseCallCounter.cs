namespace Pricing.Infrastructure.Diagnostics;

public interface IDatabaseCallCounter
{
    int Count { get; }
    IDisposable BeginRequest();
    void Increment();
}

public sealed class DatabaseCallCounter : IDatabaseCallCounter
{
    private readonly AsyncLocal<CounterState?> current = new();

    public int Count => current.Value?.Count ?? 0;

    public IDisposable BeginRequest()
    {
        CounterState? previous = current.Value;
        current.Value = new CounterState();
        return new Scope(() => current.Value = previous);
    }

    public void Increment()
    {
        if (current.Value is { } state)
        {
            state.Count++;
        }
    }

    private sealed class CounterState
    {
        public int Count { get; set; }
    }

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? action = dispose;
        public void Dispose() => Interlocked.Exchange(ref action, null)?.Invoke();
    }
}