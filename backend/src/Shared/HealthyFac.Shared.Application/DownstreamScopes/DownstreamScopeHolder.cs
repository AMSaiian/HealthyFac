namespace HealthyFac.Shared.Application.DownstreamScopes;

/// <summary>
/// Keeps the downstream scopes of the current flow in an <see cref="AsyncLocal{T}"/>. Register it as a singleton: each
/// asynchronous flow sees its own value.
/// </summary>
public sealed class DownstreamScopeHolder : IDownstreamScopeHolder
{
    private readonly AsyncLocal<IReadOnlyCollection<string>?> _current = new();

    public IReadOnlyCollection<string> Current => _current.Value ?? [];

    public IDisposable Enter(params string[] scopes)
    {
        var previous = _current.Value;
        _current.Value = scopes;

        return new DownstreamScopeLease(this, previous);
    }

    private sealed class DownstreamScopeLease(
        DownstreamScopeHolder holder,
        IReadOnlyCollection<string>? previous
    ) : IDisposable
    {
        private readonly DownstreamScopeHolder _holder = holder;
        private readonly IReadOnlyCollection<string>? _previous = previous;

        public void Dispose() => _holder._current.Value = _previous;
    }
}
