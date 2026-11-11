using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Messaging.Ingest;

/// <summary>
/// Tells Document the new ingest state of a file, so the company file grid shows it without a call to Insights. A
/// receiver ignores a state that is older than the one it holds.
/// </summary>
public sealed record IngestStateChanged : IIntegrationMessage, IRequest
{
    public static string MessageType => "insights.v1:IngestStateChanged";

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    /// <summary>One of <see cref="IngestMessageStates.All"/>.</summary>
    public required string State { get; init; }

    /// <summary>The number of attempts of the file that ended as failed.</summary>
    public required int FailedCount { get; init; }

    public required DateTime ChangedAt { get; init; }
}

public class IngestStateChangedValidator : AbstractValidator<IngestStateChanged>
{
    public IngestStateChangedValidator()
    {
        RuleFor(message => message.FileId).NotEmpty().WithMessage(IngestMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(IngestMessageErrors.Validation.IdRequired);
        RuleFor(message => message.State)
            .SingleLineText()
            .NotEmpty()
            .WithMessage(IngestMessageErrors.Validation.StateInvalid)
            .Must(state => IngestMessageStates.All.Contains(state))
            .WithMessage(IngestMessageErrors.Validation.StateInvalid);
        RuleFor(message => message.FailedCount)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .WithMessage(IngestMessageErrors.Validation.FailedCountNegative);
        RuleFor(message => message.ChangedAt)
            .NotEmpty()
            .WithMessage(IngestMessageErrors.Validation.ChangedAtRequired);
    }
}
