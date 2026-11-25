namespace HealthyFac.Telemetry.Application.Intake;

public sealed record StatusDefinitionKey
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }
}
