using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.Drafts;

/// <summary>
/// Tells Telemetry that a user dismissed a create machine draft. Telemetry dismisses the definition drafts that wait
/// for it.
/// </summary>
public sealed record MachineDraftDismissed : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:MachineDraftDismissed";

    public required Guid DraftId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class MachineDraftDismissedValidator : AbstractValidator<MachineDraftDismissed>
{
    public MachineDraftDismissedValidator()
    {
        RuleFor(message => message.DraftId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.DraftIdRequired);

        RuleFor(message => message.CompanyId)
            .NotEmpty()
            .WithMessage(DraftMessageErrors.Validation.CompanyIdRequired);
    }
}
