using HealthyFac.MachineSimulator.Scenarios.Payloads;
using HealthyFac.Seeder.TelemetryHistory.Simulation;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Seeder.TelemetryHistory.Persistence;

/// <summary>
/// Builds the rows of a history message the way the intake does: the payload of the scenario library, the parser with
/// the latest schema of the machine, and the shared mapping of the readings. A message with an error stops the seed,
/// because the history must hold messages without errors.
/// </summary>
/// <param name="parser">Reads the payload and checks it against the schema.</param>
/// <param name="data">What Telemetry holds about the machine.</param>
public sealed class HistoryMessageBuilder(IMachineMessageParser parser, HistoryMachineData data)
{
    private readonly IMachineMessageParser _parser = parser;
    private readonly HistoryMachineData _data = data;

    /// <summary>Builds the rows of one sample.</summary>
    /// <param name="sample">The sample of the simulation.</param>
    /// <param name="messageId">The ID to store for the message, or <see langword="null"/> for a new ID.</param>
    /// <returns>The rows.</returns>
    /// <exception cref="InvalidOperationException">The parser rejects the payload.</exception>
    public HistoryRows Build(HistorySample sample, Guid? messageId = null)
    {
        var built = MachinePayloadBuilder.Build(sample.Sample, _data.Definitions, sample.SampledAt);
        var payload = _parser.ReadPayload(built.Json);
        if (payload.ErrorCode is not null)
        {
            throw new InvalidOperationException(
                TelemetryHistoryErrors.PayloadRejected(_data.GatewayId, sample.SampledAt, payload.ErrorCode));
        }

        var parsed = _parser.Parse(_data.SchemaJson, payload.Text!, sample.SampledAt);
        if (parsed.IsEnvelopeFailure)
        {
            var code = parsed.Errors[index: 0].Code;

            throw new InvalidOperationException(
                TelemetryHistoryErrors.PayloadRejected(_data.GatewayId, sample.SampledAt, code));
        }

        var readings = AcceptedMessageReadings.Build(parsed, _data.MetricIdsByKey, _data.StatusDefinitions);
        if (readings.Errors.Count > 0)
        {
            var code = readings.Errors[index: 0].Code;

            throw new InvalidOperationException(
                TelemetryHistoryErrors.PayloadRejected(_data.GatewayId, sample.SampledAt, code));
        }

        return new HistoryRows
        {
            Received = new ReceivedMessage(
                _data.RegistrationId,
                $"{IntakeConstants.TopicPrefix}{_data.GatewayId}{IntakeConstants.TopicSuffix}",
                parsed.SampledAt,
                sample.SampledAt,
                _data.SchemaRevision,
                rawPayload: null,
                payload.Text,
                readings.Errors),
            MessageId = messageId,
            Numeric = readings.Numeric,
            Discrete = readings.Discrete,
            Statuses = readings.Statuses,
        };
    }
}
