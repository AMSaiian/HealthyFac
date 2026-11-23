using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace HealthyFac.Seeder.ServiceHosts;

/// <summary>
/// Replaces the Rebus bus in every seed host. The job sends messages through the outbox and the in-memory relay, so a
/// call of the bus is a bug, and the stub stops it before a broker connection opens.
/// </summary>
internal sealed class NoMessageBus : IBus
{
    public IAdvancedApi Advanced => throw NoBus();

    public Task SendLocal(object commandMessage, IDictionary<string, string>? optionalHeaders = null) =>
        throw NoBus();

    public Task Send(object commandMessage, IDictionary<string, string>? optionalHeaders = null) => throw NoBus();

    public Task DeferLocal(
        TimeSpan delay,
        object message,
        IDictionary<string, string>? optionalHeaders = null
    ) => throw NoBus();

    public Task Defer(TimeSpan delay, object message, IDictionary<string, string>? optionalHeaders = null) =>
        throw NoBus();

    public Task Reply(object replyMessage, IDictionary<string, string>? optionalHeaders = null) => throw NoBus();

    public Task Subscribe<TEvent>() => throw NoBus();

    public Task Subscribe(Type eventType) => throw NoBus();

    public Task Unsubscribe<TEvent>() => throw NoBus();

    public Task Unsubscribe(Type eventType) => throw NoBus();

    public Task Publish(object eventMessage, IDictionary<string, string>? optionalHeaders = null) => throw NoBus();

    public void Dispose()
    {
    }

    private static InvalidOperationException NoBus() => new(ServiceHostErrors.NoMessageBus);
}
