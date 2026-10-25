using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public sealed class CollectionConventionTestModel
{
    public required List<Guid> DepartmentIds { get; init; }

    public required IReadOnlyList<Guid> MachineIds { get; init; }

    public required Guid[] StepIds { get; init; }

    public IEnumerable<Guid>? OptionalIds { get; init; }

    [IgnoreIdEncryption]
    public required List<Guid> IgnoredIds { get; init; }

    public required List<Guid> CorrelationTokens { get; init; }
}
