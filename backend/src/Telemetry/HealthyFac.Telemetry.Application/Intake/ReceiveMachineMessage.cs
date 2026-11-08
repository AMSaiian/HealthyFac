using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.Intake;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// A raw MQTT message of a machine: its topic, its bytes, and the moment of receipt.
/// </summary>
public record ReceiveMachineMessageRequest : IRequest<ReceiveMachineMessageResponse>
{
    public required string Topic { get; init; }

    /// <summary>The bytes of the payload. The request log masks them, because they are machine data.</summary>
    [SensitiveData]
    public required byte[] Payload { get; init; }

    public required DateTime ReceivedAt { get; init; }
}

/// <summary>
/// The outcome of the intake for one message. The MQTT service records its trace tags and metrics from it.
/// </summary>
public record ReceiveMachineMessageResponse
{
    public required ReceiveMachineMessageResult Result { get; init; }

    /// <summary>The first envelope error code of a rejected message, else <see langword="null"/>.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>The sampling time of the message, or <see langword="null"/> when it is unreadable.</summary>
    public DateTime? SampledAt { get; init; }
}

public class ReceiveMachineMessageRequestValidator : AbstractValidator<ReceiveMachineMessageRequest>
{
    public ReceiveMachineMessageRequestValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty()
            .WithMessage(IntakeErrors.TopicRequired)
            .MaximumLength(IntakeConstants.TopicMaxLength)
            .SingleLineText();

        RuleFor(x => x.Payload)
            .NotNull()
            .WithMessage(IntakeErrors.PayloadRequired);

        RuleFor(x => x.ReceivedAt)
            .NotEmpty()
            .WithMessage(IntakeErrors.ReceivedAtRequired);
    }
}

/// <summary>
/// The intake has no company scope: the sender is a machine, and the topic names the machine.
/// </summary>
public sealed class ReceiveMachineMessagePreHandlerCheck : PassThroughPreHandlerCheck<ReceiveMachineMessageRequest>;

/// <summary>
/// Stores a machine message and its valid readings in one save. A message that breaks a rule is stored with its
/// errors and is not rejected: a wrong definition must not delete correct data.
/// </summary>
/// <remarks>
/// The handler reads the gateway ID only from the topic. A value outside the range or the states is analysis data and
/// never an error. A repeated delivery of the same machine and sampling time hits the unique index of the received
/// messages: the handler looks for a stored message first, and when a parallel delivery wins the save, the unique
/// violation rolls back the whole save and the handler reads again and succeeds. The save covers the message and the
/// readings, so the caller may acknowledge the broker message after the handler returns.
/// </remarks>
/// <param name="dbContext">Reads the machine, its schema, and its definitions, and saves the message and the
/// readings.</param>
/// <param name="machineMessageParser">Reads the raw payload and checks it against the schema.</param>
public sealed class ReceiveMachineMessageRequestHandler(
    ITelemetryDbContext dbContext,
    IMachineMessageParser machineMessageParser
) : IRequestHandler<ReceiveMachineMessageRequest, ReceiveMachineMessageResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IMachineMessageParser _machineMessageParser = machineMessageParser;

    public async Task<ReceiveMachineMessageResponse> HandleAsync(
        ReceiveMachineMessageRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var message = await BuildAsync(request, cancellationToken);

        if (await IsStoredAsync(message.Received, cancellationToken))
        {
            return ToResponse(message, ReceiveMachineMessageResult.Duplicate);
        }

        _dbContext.ReceivedMessages.Add(message.Received);
        _dbContext.NumericReadings.AddRange(message.Numeric);
        _dbContext.DiscreteReadings.AddRange(message.Discrete);
        _dbContext.StatusReadings.AddRange(message.Statuses);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            var isStoredByOther = await IsStoredAsync(message.Received, cancellationToken);

            if (!isStoredByOther)
            {
                throw;
            }

            return ToResponse(message, ReceiveMachineMessageResult.Duplicate);
        }

        return ToResponse(message, GetStoredResult(message));
    }

    private static ReceiveMachineMessageResult GetStoredResult(BuiltMessage message)
    {
        if (message.IsRejected)
        {
            return ReceiveMachineMessageResult.Rejected;
        }

        return message.Received.Errors.Count > 0
            ? ReceiveMachineMessageResult.StoredWithErrors
            : ReceiveMachineMessageResult.Stored;
    }

    private static ReceiveMachineMessageResponse ToResponse(BuiltMessage message, ReceiveMachineMessageResult result) =>
        new()
        {
            Result = result,
            ErrorCode = result == ReceiveMachineMessageResult.Rejected ? message.Received.Errors[0].Code : null,
            SampledAt = message.Received.SampledAt,
        };

    private async Task<bool> IsStoredAsync(ReceivedMessage message, CancellationToken cancellationToken)
    {
        if (message.MachineRegistrationId is null || message.SampledAt is null)
        {
            return false;
        }

        return await _dbContext.ReceivedMessages.AnyAsync(
            candidate => candidate.MachineRegistrationId == message.MachineRegistrationId
                && candidate.SampledAt == message.SampledAt,
            cancellationToken);
    }

    private async Task<BuiltMessage> BuildAsync(
        ReceiveMachineMessageRequest request,
        CancellationToken cancellationToken
    )
    {
        var gatewayId = IntakeTopic.TryGetGatewayId(request.Topic);

        if (gatewayId is null)
        {
            return Failure(request, registrationId: null, IntakeErrorCodes.MalformedGatewayId);
        }

        var registrationId = await _dbContext.MachineRegistrations
            .AsNoTracking()
            .Where(registration => registration.GatewayId == gatewayId)
            .Select(registration => (Guid?)registration.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (registrationId is null)
        {
            return Failure(request, registrationId: null, IntakeErrorCodes.UnknownGateway);
        }

        var payload = _machineMessageParser.ReadPayload(request.Payload);

        if (payload.ErrorCode is not null)
        {
            return Failure(request, registrationId, payload.ErrorCode);
        }

        var schema = await _dbContext.MetricSchemaRevisions
            .AsNoTracking()
            .Where(revision => revision.MachineRegistrationId == registrationId)
            .OrderByDescending(revision => revision.Revision)
            .Select(revision => new { revision.Revision, revision.Schema })
            .FirstAsync(cancellationToken);

        var parsed = _machineMessageParser.Parse(schema.Schema, payload.Text!, request.ReceivedAt);

        if (parsed.IsEnvelopeFailure)
        {
            return new BuiltMessage
            {
                Received = NewReceived(
                    request, registrationId, schema.Revision, parsed.SampledAt, payload.Text, parsed.Errors),
                IsRejected = true,
            };
        }

        return await BuildReadingsAsync(
            request, registrationId.Value, schema.Revision, parsed, payload.Text, cancellationToken);
    }

    private async Task<BuiltMessage> BuildReadingsAsync(
        ReceiveMachineMessageRequest request,
        Guid registrationId,
        int revision,
        ParsedMachineMessage parsed,
        string? payloadText,
        CancellationToken cancellationToken
    )
    {
        var errors = parsed.Errors.ToList();
        var sampledAt = parsed.SampledAt!.Value;

        var definitions = await _dbContext.MetricDefinitions
            .AsNoTracking()
            .Where(metric => metric.MachineRegistrationId == registrationId)
            .Select(metric => new { metric.Id, metric.Key })
            .ToDictionaryAsync(metric => metric.Key, metric => metric.Id, StringComparer.Ordinal, cancellationToken);

        var statusDefinitions = await _dbContext.StatusDefinitions
            .AsNoTracking()
            .Where(status => status.MachineRegistrationId == registrationId)
            .Select(status => new { status.Id, status.Code })
            .ToListAsync(cancellationToken);

        var numeric = new List<NumericReading>();
        var discrete = new List<DiscreteReading>();

        foreach (var value in parsed.Metrics.Where(value => definitions.ContainsKey(value.Key)))
        {
            var metricId = definitions[value.Key];

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

        return new BuiltMessage
        {
            Received = NewReceived(request, registrationId, revision, sampledAt, payloadText, errors),
            Numeric = numeric,
            Discrete = discrete,
            Statuses = statuses,
        };
    }

    private BuiltMessage Failure(ReceiveMachineMessageRequest request, Guid? registrationId, string code)
    {
        var isUnreadable = code is IntakeErrorCodes.MalformedJson or IntakeErrorCodes.UnstorableJson;
        var payload = isUnreadable ? default(PayloadReading) : _machineMessageParser.ReadPayload(request.Payload);

        return new BuiltMessage
        {
            Received = NewReceived(
                request,
                registrationId,
                schemaRevision: null,
                sampledAt: null,
                payload.Text,
                [new IntakeError { Path = string.Empty, Code = code }]),
            IsRejected = true,
        };
    }

    private static ReceivedMessage NewReceived(
        ReceiveMachineMessageRequest request,
        Guid? registrationId,
        int? schemaRevision,
        DateTime? sampledAt,
        string? payloadText,
        IReadOnlyList<IntakeError> errors
    ) =>
        new(
            registrationId,
            request.Topic,
            sampledAt,
            request.ReceivedAt,
            schemaRevision,
            payloadText is null ? request.Payload : null,
            payloadText,
            errors);

    private sealed class BuiltMessage
    {
        public required ReceivedMessage Received { get; init; }

        /// <summary>An envelope error: the message is stored with its error and no readings.</summary>
        public bool IsRejected { get; init; }

        public IReadOnlyList<NumericReading> Numeric { get; init; } = [];

        public IReadOnlyList<DiscreteReading> Discrete { get; init; } = [];

        public IReadOnlyList<StatusReading> Statuses { get; init; } = [];
    }
}
