using FluentValidation;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.Intake;

namespace HealthyFac.Telemetry.Application.Intake;

/// <summary>
/// A machine message that the intake could not process: its topic, its bytes, and the moment of receipt.
/// </summary>
public record StoreFailedMachineMessageRequest : IRequest
{
    public required string Topic { get; init; }

    /// <summary>The bytes of the payload. The request log masks them, because they are machine data.</summary>
    [SensitiveData]
    public required byte[] Payload { get; init; }

    public required DateTime ReceivedAt { get; init; }
}

public class StoreFailedMachineMessageRequestValidator : AbstractValidator<StoreFailedMachineMessageRequest>
{
    public StoreFailedMachineMessageRequestValidator()
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
public sealed class
    StoreFailedMachineMessagePreHandlerCheck : PassThroughPreHandlerCheck<StoreFailedMachineMessageRequest>;

/// <summary>
/// Stores a message that failed in a way that a repeat cannot fix. The row holds the raw bytes only, the topic, and the
/// error <see cref="IntakeErrorCodes.ProcessingFailed"/>, so no readings and no machine are linked to it.
/// </summary>
/// <param name="dbContext">Saves the message.</param>
public sealed class StoreFailedMachineMessageRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<StoreFailedMachineMessageRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        StoreFailedMachineMessageRequest request,
        CancellationToken cancellationToken
    )
    {
        _dbContext.ReceivedMessages.Add(
            new ReceivedMessage(
                machineRegistrationId: null,
                request.Topic,
                sampledAt: null,
                request.ReceivedAt,
                schemaRevision: null,
                request.Payload,
                payload: null,
                [new IntakeError { Path = string.Empty, Code = IntakeErrorCodes.ProcessingFailed }]));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
