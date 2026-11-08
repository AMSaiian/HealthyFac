using HealthyFac.Testing.Resetting;

namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

/// <summary>
/// The transport fixture as a collection fixture: it resets the database, the queues, and the recorded state before
/// every test. The single-class fixtures that derive from <see cref="TransportFixture"/> keep their own state and are
/// not reset.
/// </summary>
public sealed class TransportCollectionFixture : TransportFixture, IResetBeforeEachTest
{
    public async ValueTask ResetAsync() => await ResetStateAsync();
}
