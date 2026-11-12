using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.Application.Drafts.SearchMachineDrafts;

/// <summary>
/// The typed filter of the draft grid of a machine. A field without a value adds no condition.
/// </summary>
public sealed record MachineDraftGridFilter : IFilter<MachineDraftGridItem>
{
    /// <summary>The kinds to show: <c>machine</c>, <c>metric</c>, <c>status</c>.</summary>
    public SetFilter<string>? Kinds { get; init; }

    /// <summary>The states to show: pending, applied, dismissed.</summary>
    public SetFilter<DraftState>? States { get; init; }

    public Expression<Func<MachineDraftGridItem, bool>>? ToPredicate() =>
        Predicates.From(Kinds?.ToPredicate<MachineDraftGridItem>(item => item.Kind))
            .And(States?.ToPredicate<MachineDraftGridItem>(item => item.State));
}

public sealed class MachineDraftGridFilterValidator : AbstractValidator<MachineDraftGridFilter>
{
    public MachineDraftGridFilterValidator()
    {
        When(filter => filter.Kinds is not null, () => RuleFor(filter => filter.Kinds!).IsSetFilter());

        When(filter => filter.States is not null, () => RuleFor(filter => filter.States!).IsSetFilter());

        RuleFor(filter => filter.Kinds!.Values)
            .Must(values => values.All(kind => DraftKinds.All.Contains(kind)))
            .WithMessage(DraftErrors.KindUnknown)
            .When(filter => filter.Kinds?.Values is not null);

        RuleForEach(filter => filter.Kinds!.Values)
            .SingleLineText()
            .When(filter => filter.Kinds?.Values is not null);
    }
}
