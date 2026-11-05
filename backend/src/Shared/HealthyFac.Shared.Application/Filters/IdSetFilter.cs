using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Matches the rows whose ID column is one of the given IDs. It is a <see cref="SetFilter{TValue}"/> for IDs with a
/// property name that the ID encryption convention covers (<c>Ids</c>), so the JSON options decrypt each ID. A
/// generic property named <c>Values</c> would not match the convention.
/// </summary>
public sealed record IdSetFilter
{
    public required IReadOnlyList<Guid> Ids { get; init; }

    /// <summary>
    /// Builds the predicate "the selected ID is in the set".
    /// </summary>
    /// <param name="selector">Selects the ID column of the entity.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public Expression<Func<T, bool>> ToPredicate<T>(Expression<Func<T, Guid>> selector) =>
        new SetFilter<Guid> { Values = Ids }.ToPredicate(selector);
}

/// <summary>
/// Checks that an ID set filter holds its IDs and that no ID is empty.
/// </summary>
public sealed class IdSetFilterValidator : AbstractValidator<IdSetFilter>
{
    public IdSetFilterValidator()
    {
        RuleFor(filter => filter.Ids)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage(GridErrors.Validation.IdSetFilterIdsRequired)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage(GridErrors.Validation.IdSetFilterIdEmpty);
    }
}
