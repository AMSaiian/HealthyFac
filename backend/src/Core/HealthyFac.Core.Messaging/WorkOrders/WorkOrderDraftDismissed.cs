using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.WorkOrders;

/// <summary>
/// Tells Insights that a user dismissed a work order draft. Insights marks the signals of the case that the draft
/// covers as dismissed.
/// </summary>
public sealed record WorkOrderDraftDismissed : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:WorkOrderDraftDismissed";

    public required Guid DraftId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid CompanyId { get; init; }

    public required DateTime DismissedAt { get; init; }
}

public class WorkOrderDraftDismissedValidator : AbstractValidator<WorkOrderDraftDismissed>
{
    public WorkOrderDraftDismissedValidator()
    {
        RuleFor(message => message.DraftId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CaseId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.DismissedAt)
            .NotEmpty()
            .WithMessage(WorkOrderDraftMessageErrors.Validation.TimeRequired);
    }
}
