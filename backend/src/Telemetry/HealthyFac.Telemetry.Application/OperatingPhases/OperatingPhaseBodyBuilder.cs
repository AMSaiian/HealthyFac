using FluentValidation.Results;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// Checks the stateful rules of a phase body against the definitions of the machine, and builds the conditions and
/// the limit rows. The create and the update use case share it. The domain throws for a broken rule, so this class
/// reports every broken rule first, each with the key of its field.
/// </summary>
public static class OperatingPhaseBodyBuilder
{
    private static readonly string ConditionsKey = ValidationNames.GetJsonName(
        typeof(OperatingPhaseInput).GetProperty(nameof(OperatingPhaseInput.Conditions))!);

    private static readonly string LimitsKey = ValidationNames.GetJsonName(
        typeof(OperatingPhaseInput).GetProperty(nameof(OperatingPhaseInput.Limits))!);

    private static readonly string OperatorKey = ValidationNames.GetJsonName(
        typeof(PhaseConditionInput).GetProperty(nameof(PhaseConditionInput.Operator))!);

    private static readonly string StatesKey = ValidationNames.GetJsonName(
        typeof(PhaseConditionInput).GetProperty(nameof(PhaseConditionInput.States))!);

    private static readonly string MetricKey = ValidationNames.GetJsonName(
        typeof(PhaseLimitInput).GetProperty(nameof(PhaseLimitInput.MetricDefinitionId))!);

    extension(ITelemetryDbContext dbContext)
    {
        /// <summary>
        /// Builds the conditions and the limit rows of a body from the definitions of the machine.
        /// </summary>
        /// <param name="input">The body of the request.</param>
        /// <param name="machineId">The machine of the phase.</param>
        /// <param name="cancellationToken">Cancels the queries.</param>
        /// <returns>The conditions and the limit rows. Both pass the domain rules.</returns>
        /// <exception cref="AppValidationException">A source, an operator, a state, or a limit row breaks a
        /// rule.</exception>
        public async Task<(IReadOnlyList<OperatingPhaseCondition> Conditions,
                IReadOnlyList<OperatingPhaseLimit> Limits)>
            BuildPhaseBodyAsync(OperatingPhaseInput input, Guid machineId, CancellationToken cancellationToken)
        {
            var metrics = await dbContext.MetricDefinitions
                .AsNoTracking()
                .Where(metric => metric.MachineRegistrationId == machineId)
                .ToDictionaryAsync(metric => metric.Id, cancellationToken);

            var statusIds = await dbContext.StatusDefinitions
                .AsNoTracking()
                .Where(status => status.MachineRegistrationId == machineId)
                .Select(status => status.Id)
                .ToListAsync(cancellationToken);

            var failures = new List<ValidationFailure>();
            var conditions = new List<OperatingPhaseCondition>();

            for (var index = 0; index < input.Conditions.Count; index++)
            {
                var condition = input.Conditions[index];
                var key = $"{ConditionsKey}[{index}]";
                var source = ResolveSource(condition, metrics, statusIds);

                if (source is null)
                {
                    failures.Add(new ValidationFailure(key, OperatingPhaseErrors.SourceUnknown));

                    continue;
                }

                if (!condition.Operator!.Accepts(source))
                {
                    failures.Add(
                        new ValidationFailure($"{key}.{OperatorKey}", OperatingPhaseErrors.OperatorDoesNotFit));

                    continue;
                }

                if (condition.Operator == PhaseConditionOperator.In
                    && condition.States!.Any(state => source.States?.Contains(state) != true))
                {
                    failures.Add(new ValidationFailure($"{key}.{StatesKey}", OperatingPhaseErrors.StateUnknown));

                    continue;
                }

                conditions.Add(
                    OperatingPhaseCondition.Create(
                        source,
                        condition.Operator,
                        condition.Value,
                        condition.UpperValue,
                        condition.States));
            }

            var conditionMetricIds = input.Conditions
                .Where(condition => condition.MetricDefinitionId.HasValue)
                .Select(condition => condition.MetricDefinitionId!.Value)
                .ToHashSet();

            var limits = new List<OperatingPhaseLimit>();

            for (var index = 0; index < input.Limits.Count; index++)
            {
                var limit = input.Limits[index];
                var key = $"{LimitsKey}[{index}].{MetricKey}";

                if (!metrics.TryGetValue(limit.MetricDefinitionId, out var metric))
                {
                    failures.Add(new ValidationFailure(key, OperatingPhaseErrors.LimitMetricUnknown));
                }
                else if (metric.ValueKind != MetricValueKind.Numeric)
                {
                    failures.Add(new ValidationFailure(key, OperatingPhaseErrors.LimitMetricNotNumeric));
                }
                else if (conditionMetricIds.Contains(limit.MetricDefinitionId))
                {
                    failures.Add(new ValidationFailure(key, OperatingPhaseErrors.LimitOnConditionMetric));
                }
                else
                {
                    limits.Add(
                        OperatingPhaseLimit.Create(
                            limit.MetricDefinitionId,
                            metric.ValueKind,
                            new MetricLimits
                            {
                                WarningLow = limit.WarningLow,
                                WarningHigh = limit.WarningHigh,
                                AlarmLow = limit.AlarmLow,
                                AlarmHigh = limit.AlarmHigh,
                            }));
                }
            }

            if (failures.Count > 0)
            {
                throw new AppValidationException(failures);
            }

            return (conditions, limits);
        }
    }

    private static PhaseConditionSource? ResolveSource(
        PhaseConditionInput condition,
        IReadOnlyDictionary<Guid, MetricDefinition> metrics,
        IReadOnlyList<Guid> statusIds
    )
    {
        if (condition.StatusDefinitionId is { } statusId)
        {
            return statusIds.Contains(statusId) ? PhaseConditionSource.Status(statusId) : null;
        }

        return condition.MetricDefinitionId is { } metricId && metrics.TryGetValue(metricId, out var metric)
            ? PhaseConditionSource.Metric(metricId, metric.ValueKind, metric.States)
            : null;
    }
}
