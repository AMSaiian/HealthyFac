using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// The phase as the API shows it: the result of a get and an item of the list.
/// </summary>
public record OperatingPhaseResponse
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required int Priority { get; init; }

    public required bool IsTracked { get; init; }

    public required IReadOnlyList<PhaseConditionResponse> Conditions { get; init; }

    public required IReadOnlyList<PhaseLimitResponse> Limits { get; init; }

    public required DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Maps a loaded phase with its conditions and limit rows.
    /// </summary>
    /// <param name="phase">The phase.</param>
    /// <returns>The response.</returns>
    public static OperatingPhaseResponse From(OperatingPhase phase) =>
        new()
        {
            Id = phase.Id,
            Code = phase.Code,
            Name = phase.Name,
            Description = phase.Description,
            Priority = phase.Priority,
            IsTracked = phase.IsTracked,
            Conditions = phase.Conditions
                .Select(condition => new PhaseConditionResponse
                {
                    MetricDefinitionId = condition.MetricDefinitionId,
                    StatusDefinitionId = condition.StatusDefinitionId,
                    Operator = condition.Operator,
                    Value = condition.Value,
                    UpperValue = condition.UpperValue,
                    States = condition.States,
                })
                .ToList(),
            Limits = phase.Limits
                .Select(limit => new PhaseLimitResponse
                {
                    MetricDefinitionId = limit.MetricDefinitionId,
                    WarningLow = limit.WarningLow,
                    WarningHigh = limit.WarningHigh,
                    AlarmLow = limit.AlarmLow,
                    AlarmHigh = limit.AlarmHigh,
                })
                .ToList(),
            CreatedAt = phase.CreatedAt,
            UpdatedAt = phase.UpdatedAt,
        };
}

public record PhaseConditionResponse
{
    public Guid? MetricDefinitionId { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public required PhaseConditionOperator Operator { get; init; }

    public double? Value { get; init; }

    public double? UpperValue { get; init; }

    public IReadOnlyList<string>? States { get; init; }
}

public record PhaseLimitResponse
{
    public required Guid MetricDefinitionId { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }
}
