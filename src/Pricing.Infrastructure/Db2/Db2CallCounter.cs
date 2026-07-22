namespace Pricing.Infrastructure.Db2;

public interface IDb2CallCounter
{
    int Count { get; }
    IDisposable BeginRequest();
    void Increment();
}

public sealed class Db2CallCounter : IDb2CallCounter
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
