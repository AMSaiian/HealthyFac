using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// Maps the parsed values of an accepted machine message to readings. The intake and the seeder share this mapping.
/// </summary>
public static class AcceptedMessageReadings
{
    /// <summary>
    /// A value of a metric without a definition is dropped. A status code without a definition adds an error and no
    /// reading.
    /// </summary>
    /// <param name="parsed">The message that passed the envelope check.</param>
    /// <param name="metricIdsByKey">The metric definition IDs of the machine, by metric key.</param>
    /// <param name="statusDefinitions">The status definitions of the machine.</param>
    /// <returns>The readings and the errors of the message.</returns>
    public static AcceptedReadings Build(
        ParsedMachineMessage parsed,
        IReadOnlyDictionary<string, Guid> metricIdsByKey,
        IReadOnlyList<StatusDefinitionKey> statusDefinitions
    )
    {
        var sampledAt = parsed.SampledAt!.Value;
        var errors = parsed.Errors.ToList();
        var numeric = new List<NumericReading>();
        var discrete = new List<DiscreteReading>();

        foreach (var value in parsed.Metrics.Where(value => metricIdsByKey.ContainsKey(value.Key)))
        {
            var metricId = metricIdsByKey[value.Key];

            if (value.Number is { } number)
            {
                numeric.Add(new NumericReading(metricId, sampledAt, number, value.IsOutOfRange));
            }
            else
            {
                discrete.Add(new DiscreteReading(metricId, sampledAt, value.Text!, value.IsOutOfRange));
            }
        }

        var statuses = new List<StatusReading>();

        foreach (var code in parsed.StatusCodes)
        {
            var definition = statusDefinitions.FirstOrDefault(candidate => string.Equals(
                candidate.Code, code, StringComparison.OrdinalIgnoreCase));

            if (definition is null)
            {
                errors.Add(
                    new IntakeError
                    {
                        Path = $"{MetricSchemaConstants.Fields.Statuses}.{code}", Code = IntakeErrorCodes.UnknownStatus,
                    });

                continue;
            }

            statuses.Add(new StatusReading(definition.Id, sampledAt));
        }

        return new AcceptedReadings
        {
            Numeric = numeric,
            Discrete = discrete,
            Statuses = statuses,
            Errors = errors,
        };
    }
}
