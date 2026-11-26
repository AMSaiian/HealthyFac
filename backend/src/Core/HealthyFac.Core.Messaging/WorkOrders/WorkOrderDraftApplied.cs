using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Messaging.WorkOrders;

/// <summary>
/// Tells Insights that a user applied a work order draft: the work order exists. Insights marks the signals of the
/// case that the draft covers as linked to the work order.
/// </summary>
public sealed record WorkOrderDraftApplied : IIntegrationMessage, IRequest
{
    public static string MessageType => "core.v1:WorkOrderDraftApplied";

    public required Guid DraftId { get; init; }

    public required Guid WorkOrderId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid CompanyId { get; init; }

    public required DateTime AppliedAt { get; init; }
}

public class WorkOrderDraftAppliedValidator : AbstractValidator<WorkOrderDraftApplied>
{
    public WorkOrderDraftAppliedValidator()
    {
        RuleFor(message => message.DraftId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

        RuleFor(message => message.WorkOrderId)
            .NotEmpty()
            .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

        RuleFor(message => message.CaseId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

        RuleFor(message => message.AppliedAt)
            .NotEmpty()
            .WithMessage(WorkOrderDraftMessageErrors.Validation.TimeRequired);
    }
}
