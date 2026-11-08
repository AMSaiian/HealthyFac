namespace HealthyFac.Shared.IntegrationTests.TestReset.Data;

public sealed class ReferenceItem
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Name { get; init; }
}
