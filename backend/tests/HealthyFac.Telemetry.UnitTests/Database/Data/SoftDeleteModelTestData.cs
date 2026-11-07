using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Infrastructure.MachineRegistrations.Persistence;

namespace HealthyFac.Telemetry.UnitTests.Database.Data;

public static class SoftDeleteModelTestData
{
    /// <summary>
    /// The unique indexes that keep deleted rows on purpose, as <c>Entity.index_name</c>. Each one covers a value that
    /// is never reused after a delete. A new unfiltered unique index needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> UnfilteredUniqueIndexes =
    [
        $"{nameof(MachineRegistration)}.{MachineRegistrationIndexNames.GatewayId}",
    ];
}
