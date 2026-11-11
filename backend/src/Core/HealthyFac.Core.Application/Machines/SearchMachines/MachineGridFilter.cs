using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Core.Application.Machines.SearchMachines;

/// <summary>
/// The typed filter of the machine grid. A field without a value adds no condition.
/// </summary>
public sealed record MachineGridFilter : IFilter<Machine>
{
    public TextFilter? Name { get; init; }

    public TextFilter? SerialNumber { get; init; }

    public SetFilter<MachineState>? States { get; init; }

    public DateRangeFilter? CreatedAt { get; init; }

    public DateRangeFilter? UpdatedAt { get; init; }

    /// <summary>
    /// <see langword="true"/> for machines with pending drafts, <see langword="false"/> for machines without. The
    /// grid handler applies it, because it needs the draft tables (see <see cref="MachinePendingDrafts"/>).
    /// </summary>
    public bool? HasPendingDrafts { get; init; }

    public Expression<Func<Machine, bool>>? ToPredicate() =>
        Predicates.From(Name?.ToPredicate<Machine>(machine => machine.Name))
            .And(SerialNumber?.ToPredicate<Machine>(machine => machine.SerialNumber))
            .And(States?.ToPredicate(MachineCurrentState.Selector))
            .And(CreatedAt?.ToPredicate<Machine>(machine => machine.CreatedAt))
            .And(UpdatedAt?.ToPredicate<Machine>(machine => machine.UpdatedAt));
}

public sealed class MachineGridFilterValidator : AbstractValidator<MachineGridFilter>
{
    public MachineGridFilterValidator()
    {
        RuleFor(filter => filter.Name!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.Name is not null);

        RuleFor(filter => filter.Name!.Value)
            .MaximumLength(MachineValidationConstants.NameMaxLength)
            .When(filter => filter.Name?.Value is not null);

        RuleFor(filter => filter.SerialNumber!)
            .SetValidator(new TextFilterValidator())
            .When(filter => filter.SerialNumber is not null);

        RuleFor(filter => filter.SerialNumber!.Value)
            .MaximumLength(MachineValidationConstants.SerialNumberMaxLength)
            .When(filter => filter.SerialNumber?.Value is not null);

        RuleFor(filter => filter.States!)
            .SetValidator(new SetFilterValidator<MachineState>())
            .When(filter => filter.States is not null);

        RuleFor(filter => filter.CreatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.CreatedAt is not null);

        RuleFor(filter => filter.UpdatedAt!)
            .SetValidator(new DateRangeFilterValidator())
            .When(filter => filter.UpdatedAt is not null);
    }
}
