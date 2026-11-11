using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Core.Application.WorkOrders.SearchWorkOrders;

/// <summary>
/// The typed filter of the work order grid. A field without a value adds no condition. The filter on
/// <see cref="IsDelayed"/> needs the current time, so the handler applies it.
/// </summary>
public sealed record WorkOrderGridFilter : IFilter<WorkOrder>
{
    public TextFilter? Title { get; init; }

    public SetFilter<WorkOrderStatus>? Statuses { get; init; }

    public SetFilter<WorkOrderPriority>? Priorities { get; init; }

    public SetFilter<WorkOrderCategory>? Categories { get; init; }

    public IdSetFilter? Assignee { get; init; }

    public IdSetFilter? Machine { get; init; }

    public DateRangeFilter? PlannedStart { get; init; }

    public bool? IsDelayed { get; init; }

    public Expression<Func<WorkOrder, bool>>? ToPredicate() =>
        Predicates.From(Title?.ToPredicate<WorkOrder>(workOrder => workOrder.Title))
            .And(Statuses?.ToPredicate(WorkOrderCurrentRow.Of((_, row) => row.Status)))
            .And(Priorities?.ToPredicate<WorkOrder>(workOrder => workOrder.Priority))
            .And(Categories?.ToPredicate<WorkOrder>(workOrder => workOrder.Category))
            .And(Assignee?.ToPredicate(WorkOrderCurrentRow.Of((_, row) => row.AssigneeId ?? Guid.Empty)))
            .And(Machine?.ToPredicate<WorkOrder>(workOrder => workOrder.MachineId ?? Guid.Empty))
            .And(PlannedStart?.ToPredicate(WorkOrderCurrentRow.Of((_, row) => row.PlannedStartUtc)));
}

public sealed class WorkOrderGridFilterValidator : AbstractValidator<WorkOrderGridFilter>
{
    public WorkOrderGridFilterValidator()
    {
        RuleFor(filter => filter.Title!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Title is not null);

        RuleFor(filter => filter.Title!.Value)
            .MaximumLength(WorkOrderValidationConstants.TitleMaxLength)
            .When(filter => filter.Title?.Value is not null);

        RuleFor(filter => filter.Statuses!)
            .SetValidator(new SetFilterValidator<WorkOrderStatus>())
            .When(filter => filter.Statuses is not null);

        RuleFor(filter => filter.Priorities!)
            .SetValidator(new SetFilterValidator<WorkOrderPriority>())
            .When(filter => filter.Priorities is not null);

        RuleFor(filter => filter.Categories!)
            .SetValidator(new SetFilterValidator<WorkOrderCategory>())
            .When(filter => filter.Categories is not null);

        RuleFor(filter => filter.Assignee!)
            .SetValidator(new IdSetFilterValidator())
            .When(filter => filter.Assignee is not null);

        RuleFor(filter => filter.Machine!)
            .SetValidator(new IdSetFilterValidator())
            .When(filter => filter.Machine is not null);

        RuleFor(filter => filter.PlannedStart!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.PlannedStart is not null);
    }
}
