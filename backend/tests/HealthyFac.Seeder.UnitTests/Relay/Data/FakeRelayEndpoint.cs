using HealthyFac.Seeder.Relay;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;

namespace HealthyFac.Seeder.UnitTests.Relay.Data;

/// <summary>
/// A relay endpoint that keeps its rows in memory. It writes each delivery and each publish to a shared log, so a
/// test reads the order across services.
/// </summary>
public sealed class FakeRelayEndpoint : IRelayEndpoint
{
    private readonly HashSet<string> _handledTypes;
    private readonly List<string> _log;
    private readonly List<OutboxMessage> _pending = [];
    private readonly List<OutboxMessage> _inbox = [];
    private readonly Dictionary<string, string> _failures = [];
    private readonly Dictionary<Guid, string> _receivedTypes = [];

    public FakeRelayEndpoint(string service, IEnumerable<string> handledTypes, List<string> log)
    {
        Service = service;
        _handledTypes = [.. handledTypes];
        _log = log;
    }

    public string Service { get; }

    public IReadOnlyList<OutboxMessage> Pending => _pending;

    /// <summary>Called for each inbox row while the endpoint drains. The handler may add pending rows.</summary>
    public Action<OutboxMessage, FakeRelayEndpoint>? OnHandle { get; set; }

    public void AddPending(OutboxMessage row) => _pending.Add(row);

    public void FailOn(string type, string error) => _failures[type] = error;

    public Task<OutboxMessage?> FindOldestPendingAsync(CancellationToken cancellationToken)
    {
        var oldest = _pending.OrderBy(row => row.OccurredOn).ThenBy(row => row.Id).FirstOrDefault();

        return Task.FromResult(oldest);
    }

    public bool Handles(string type) => _handledTypes.Contains(type);

    public Task AddToInboxAsync(OutboxMessage row, CancellationToken cancellationToken)
    {
        _inbox.Add(row);
        _receivedTypes[row.Id] = row.Type;
        _log.Add($"{Service} received {row.Type}");

        return Task.CompletedTask;
    }

    public Task MarkPublishedAsync(Guid rowId, CancellationToken cancellationToken)
    {
        var row = _pending.Single(pending => pending.Id == rowId);
        _pending.Remove(row);
        _log.Add($"{Service} published {row.Type}");

        return Task.CompletedTask;
    }

    public Task DrainAsync(CancellationToken cancellationToken)
    {
        foreach (var row in _inbox.ToList())
        {
            OnHandle?.Invoke(row, this);
        }

        _inbox.Clear();

        return Task.CompletedTask;
    }

    public Task<string?> FindFailureAsync(Guid rowId, CancellationToken cancellationToken)
    {
        var isFailed = _receivedTypes.TryGetValue(rowId, out var type) && _failures.ContainsKey(type);

        return Task.FromResult(isFailed ? _failures[_receivedTypes[rowId]] : null);
    }
}
