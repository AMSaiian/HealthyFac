using FluentValidation;
using HealthyFac.Insights.Messaging.Drafts;
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

    public required WorkOrderProposalSource Source { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? RunId { get; init; }

    public Guid? ChatId { get; init; }

    public Guid? ChatMessageId { get; init; }

    /// <summary>The display name of the chat owner. Required for the source chat, absent for the others.</summary>
    public string? RequestedByName { get; init; }

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
        RuleFor(message => message.Source).NotNull().WithMessage(WorkOrderDraftMessageErrors.Validation.SourceRequired);

        RuleFor(message => message.MachineId)
            .NotEqual(Guid.Empty)
            .When(message => message.MachineId is not null)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

        When(
            message => message.Source == WorkOrderProposalSource.Case,
            () =>
            {
                RuleFor(message => message.MachineId)
                    .MustBeSetId()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.CaseId)
                    .MustBeSetId()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.RunId)
                    .MustBeSetId()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.ChatId)
                    .Null()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.ChatMessageId)
                    .Null()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.RequestedByName)
                    .Null()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource);
            });

        When(
            message => message.Source == WorkOrderProposalSource.Chat,
            () =>
            {
                RuleFor(message => message.ChatId)
                    .MustBeSetId()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.ChatMessageId)
                    .MustBeSetId()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.CaseId)
                    .Null()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.RunId)
                    .Null()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.RequestedByName)
                    .NotEmpty()
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.RequestedByNameRequired)
                    .SingleLineText()
                    .MaximumLength(DraftMessageLimits.RequestedByNameMaxLength)
                    .WithMessage(WorkOrderDraftMessageErrors.Validation.RequestedByNameTooLong);
            });

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
