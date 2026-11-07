using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Infrastructure.Machines.Persistence;

namespace HealthyFac.Core.UnitTests.Database.Data;

public static class SoftDeleteModelTestData
{
    /// <summary>
    /// The unique indexes that keep deleted rows on purpose, as <c>Entity.index_name</c>. Each one covers a value that
    /// is never reused after a delete. A new unfiltered unique index needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> UnfilteredUniqueIndexes =
    [
        $"{nameof(Machine)}.{MachineIndexNames.GatewayId}",
    ];
}
