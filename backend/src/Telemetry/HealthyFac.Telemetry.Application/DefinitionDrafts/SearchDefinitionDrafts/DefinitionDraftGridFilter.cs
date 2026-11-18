using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;

/// <summary>
/// The typed filter of the definition draft grid. A field without a value adds no condition.
/// </summary>
public sealed record DefinitionDraftGridFilter : IFilter<DefinitionDraftGridItem>
{
    /// <summary>The kinds to show: <c>metric</c>, <c>status</c>.</summary>
    public SetFilter<string>? Kinds { get; init; }

    /// <summary>The states to show: awaitingTarget, pending, applied, dismissed.</summary>
    public SetFilter<DefinitionDraftState>? States { get; init; }

    public Expression<Func<DefinitionDraftGridItem, bool>>? ToPredicate() =>
        Predicates.From(Kinds?.ToPredicate<DefinitionDraftGridItem>(item => item.Kind))
            .And(States?.ToPredicate<DefinitionDraftGridItem>(item => item.State));
}

public sealed class DefinitionDraftGridFilterValidator : AbstractValidator<DefinitionDraftGridFilter>
{
    public DefinitionDraftGridFilterValidator()
    {
        When(filter => filter.Kinds is not null, () => RuleFor(filter => filter.Kinds!).IsSetFilter());

        When(filter => filter.States is not null, () => RuleFor(filter => filter.States!).IsSetFilter());

        RuleFor(filter => filter.Kinds!.Values)
            .Must(values => values.All(kind => DefinitionDraftKinds.All.Contains(kind)))
            .WithMessage(DefinitionDraftErrors.KindUnknown)
            .When(filter => filter.Kinds?.Values is not null);

        RuleForEach(filter => filter.Kinds!.Values)
            .SingleLineText()
            .When(filter => filter.Kinds?.Values is not null);
    }
}
