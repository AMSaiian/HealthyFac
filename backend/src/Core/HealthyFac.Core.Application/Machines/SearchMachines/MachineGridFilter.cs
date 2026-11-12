using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Core.Application.Machines.SearchMachines;

/// <summary>
/// The typed filter of the machine grid. A field without a value adds no condition.
/// </summary>
public sealed record MachineGridFilter : IFilter<Machine>
{
    public TextFilter? Name { get; init; }

    public TextFilter? SerialNumber { get; init; }

    public TextFilter? GatewayId { get; init; }

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
            .And(GatewayId?.ToPredicate<Machine>(machine => machine.GatewayId))
            .And(States?.ToPredicate(MachineCurrentState.Selector))
            .And(CreatedAt?.ToPredicate<Machine>(machine => machine.CreatedAt))
            .And(UpdatedAt?.ToPredicate<Machine>(machine => machine.UpdatedAt));
}

public sealed class MachineGridFilterValidator : AbstractValidator<MachineGridFilter>
{
    public MachineGridFilterValidator()
    {
        When(
            filter => filter.Name is not null,
            () => RuleFor(filter => filter.Name!).IsTextFilter(MachineValidationConstants.NameMaxLength));

        When(
            filter => filter.SerialNumber is not null,
            () => RuleFor(filter => filter.SerialNumber!).IsTextFilter(MachineValidationConstants.SerialNumberMaxLength));

        When(
            filter => filter.GatewayId is not null,
            () => RuleFor(filter => filter.GatewayId!).IsTextFilter(GatewayIds.Length));

        When(filter => filter.States is not null, () => RuleFor(filter => filter.States!).IsSetFilter());

        When(filter => filter.CreatedAt is not null, () => RuleFor(filter => filter.CreatedAt!).IsDateRangeFilter());

        When(filter => filter.UpdatedAt is not null, () => RuleFor(filter => filter.UpdatedAt!).IsDateRangeFilter());
    }
}
