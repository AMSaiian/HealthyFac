using FluentValidation;

namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// The shared shape of a keyset grid request: a page starts after a position instead of at an offset.
/// </summary>
/// <typeparam name="TFilter">The typed filter record of the grid.</typeparam>
/// <typeparam name="TSortColumn">The enumeration of the sortable columns of the grid.</typeparam>
public abstract record KeysetGridRequest<TFilter, TSortColumn>
{
    public required int Limit { get; init; }

    public GridSort<TSortColumn>? Sort { get; init; }

    public TFilter? Filter { get; init; }

    /// <summary>
    /// The position of the last row of the previous page, or <see langword="null"/> for the first page.
    /// </summary>
    public KeysetPosition? After { get; init; }
}

/// <summary>
/// Validates the limit and the sort column of a keyset grid request.
/// </summary>
/// <typeparam name="TRequest">The keyset grid request type.</typeparam>
/// <typeparam name="TFilter">The typed filter record of the grid.</typeparam>
/// <typeparam name="TSortColumn">The enumeration of the sortable columns of the grid.</typeparam>
public abstract class KeysetGridRequestValidator<TRequest, TFilter, TSortColumn> : AbstractValidator<TRequest>
    where TRequest : KeysetGridRequest<TFilter, TSortColumn>
    where TSortColumn : notnull
{
    /// <param name="sortableColumns">The columns that the sort map holds.</param>
    protected KeysetGridRequestValidator(IEnumerable<TSortColumn> sortableColumns)
    {
        var sortable = sortableColumns.ToHashSet();

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);

        RuleFor(request => request.Sort!.Column)
            .Must(sortable.Contains)
            .When(request => request.Sort is not null)
            .WithMessage(GridErrors.Validation.SortColumnUnknown);
    }
}
