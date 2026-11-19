using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Messaging.WorkOrders;

/// <summary>
/// Tells Core that the planner agent of Insights proposes a work order for a machine. Core stores it as a draft that
/// a user reviews, changes, and applies or dismisses. The draft ID is the ID of the agent action, so a repeat of the
/// message makes no second draft.
/// </summary>
public sealed record WorkOrderDraftProposed : IIntegrationMessage, IRequest
{
    public static string MessageType => "insights.v1:WorkOrderDraftProposed";

    public required Guid DraftId { get; init; }

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid RunId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    /// <summary>One of <see cref="WorkOrderDraftMessageLimits.Priorities.All"/>.</summary>
    public required string Priority { get; init; }

    /// <summary>One of <see cref="WorkOrderDraftMessageLimits.Categories.All"/>.</summary>
    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}

public class WorkOrderDraftProposedValidator : AbstractValidator<WorkOrderDraftProposed>
{
    public WorkOrderDraftProposedValidator()
    {
        RuleFor(message => message.DraftId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.MachineId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CaseId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.RunId).NotEmpty().WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

        RuleFor(message => message.Title)
            .NotEmpty()
            .WithMessage(WorkOrderDraftMessageErrors.Validation.TitleRequired)
            .SingleLineText()
            .MaximumLength(WorkOrderDraftMessageLimits.TitleMaxLength)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.TitleTooLong);

        RuleFor(message => message.Description)
            .MultilineText()
            .MaximumLength(WorkOrderDraftMessageLimits.DescriptionMaxLength)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.DescriptionTooLong);

        RuleFor(message => message.Priority)
            .SingleLineText()
            .Must(priority => WorkOrderDraftMessageLimits.Priorities.All.Contains(priority))
            .WithMessage(WorkOrderDraftMessageErrors.Validation.PriorityInvalid);

        RuleFor(message => message.Category)
            .SingleLineText()
            .Must(category => WorkOrderDraftMessageLimits.Categories.All.Contains(category))
            .WithMessage(WorkOrderDraftMessageErrors.Validation.CategoryInvalid);

        RuleFor(message => message.EstimateMinutes)
            .InclusiveBetween(
                WorkOrderDraftMessageLimits.EstimateMinMinutes,
                WorkOrderDraftMessageLimits.EstimateMaxMinutes)
            .Must(estimate => estimate % WorkOrderDraftMessageLimits.EstimateStepMinutes == 0)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.EstimateInvalid);
    }
}
