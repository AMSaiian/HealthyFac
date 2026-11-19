using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Telemetry.Messaging.Signals;

/// <summary>
/// Tells the services that follow the signals (Insights) that an episode opened, escalated, or resolved. The message
/// holds the whole episode, so a consumer needs no other call. A consumer drops a message whose
/// <see cref="Sequence"/> is not above the stored one. The message holds no personal data.
/// </summary>
public sealed record SignalEpisodeChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "telemetry.v1:SignalEpisodeChanged";

    public required Guid EpisodeId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required SignalRuleValue Rule { get; init; }

    public required SignalSeverityValue Severity { get; init; }

    public required SignalEpisodeStateValue State { get; init; }

    public Guid? MetricDefinitionId { get; init; }

    public string? MetricKey { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    /// <summary>1 at the open, and 1 more for each escalate and for the resolve.</summary>
    public required int Sequence { get; init; }

    public required DateTime ChangedAt { get; init; }
}

public class SignalEpisodeChangedValidator : AbstractValidator<SignalEpisodeChanged>
{
    public SignalEpisodeChangedValidator()
    {
        RuleFor(message => message.EpisodeId)
            .NotEmpty()
            .WithMessage(SignalMessageErrors.Validation.EpisodeIdRequired);

        RuleFor(message => message.MachineId)
            .NotEmpty()
            .WithMessage(SignalMessageErrors.Validation.MachineIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(SignalMessageErrors.Validation.CompanyIdRequired);

        RuleFor(message => message.Rule)
            .NotNull()
            .WithMessage(SignalMessageErrors.Validation.RuleRequired);

        RuleFor(message => message.Severity)
            .NotNull()
            .WithMessage(SignalMessageErrors.Validation.SeverityRequired);

        RuleFor(message => message.State)
            .NotNull()
            .WithMessage(SignalMessageErrors.Validation.StateRequired);

        RuleFor(message => message.MetricKey)
            .SingleLineText()
            .MaximumLength(SignalMessageLimits.KeyMaxLength);

        RuleFor(message => message.PhaseCode)
            .SingleLineText()
            .MaximumLength(SignalMessageLimits.KeyMaxLength);

        RuleFor(message => message.StatusCode)
            .SingleLineText()
            .MaximumLength(SignalMessageLimits.StatusCodeMaxLength);

        RuleFor(message => message.OpenedAt)
            .NotEmpty()
            .WithMessage(SignalMessageErrors.Validation.OpenedAtRequired);

        RuleFor(message => message.ChangedAt)
            .NotEmpty()
            .WithMessage(SignalMessageErrors.Validation.ChangedAtRequired);

        RuleFor(message => message.Sequence)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithMessage(SignalMessageErrors.Validation.SequenceTooLow);

        RuleFor(message => message.ResolvedAt)
            .Null()
            .When(message => message.State == SignalEpisodeStateValue.Open)
            .WithMessage(SignalMessageErrors.Validation.ResolvedAtWithOpenState);

        RuleFor(message => message.ResolvedAt)
            .NotNull()
            .When(message => message.State == SignalEpisodeStateValue.Resolved)
            .WithMessage(SignalMessageErrors.Validation.ResolvedAtRequired);
    }
}
