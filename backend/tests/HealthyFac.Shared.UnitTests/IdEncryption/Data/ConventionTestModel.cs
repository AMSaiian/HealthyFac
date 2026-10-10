using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Shared.UnitTests.IdEncryption.Data;

public sealed class ConventionTestModel
{
    public required Guid Id { get; init; }

    public required Guid MachineId { get; init; }

    public Guid? TenantId { get; init; }

    [IgnoreIdEncryption]
    public required Guid IgnoredId { get; init; }

    public required Guid CorrelationToken { get; init; }

    public required int StepId { get; init; }
}
