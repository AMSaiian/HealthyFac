using FluentValidation;

namespace HealthyFac.Shared.Application.Grids;

/// <summary>
/// The shared shape of a grid request. A use case request derives from it and implements
/// <c>IRequest&lt;GridResponse&lt;TItem&gt;&gt;</c>, so every use case keeps its own request type.
/// </summary>
/// <typeparam name="TFilter">The typed filter record of the grid.</typeparam>
/// <typeparam name="TSortColumn">The enumeration of the sortable columns of the grid.</typeparam>
public abstract record GridRequest<TFilter, TSortColumn>
{
    public required int Offset { get; init; }

    public required int Limit { get; init; }

    public GridSort<TSortColumn>? Sort { get; init; }

    public required TFilter Filter { get; init; }
}

/// <summary>
/// Validates the paging, the sort column, and the presence of the filter of a grid request. A grid validator derives
/// from it, passes its sortable columns, and adds the rules of its own filter.
/// </summary>
/// <typeparam name="TRequest">The grid request type.</typeparam>
/// <typeparam name="TFilter">The typed filter record of the grid.</typeparam>
/// <typeparam name="TSortColumn">The enumeration of the sortable columns of the grid.</typeparam>
public abstract class GridRequestValidator<TRequest, TFilter, TSortColumn> : AbstractValidator<TRequest>
    where TRequest : GridRequest<TFilter, TSortColumn>
    where TSortColumn : notnull
{
    /// <param name="sortableColumns">The columns that the sort map holds.</param>
    protected GridRequestValidator(IEnumerable<TSortColumn> sortableColumns)
    {
        var sortable = sortableColumns.ToHashSet();

        RuleFor(request => request.Offset)
            .GreaterThanOrEqualTo(GridValidationConstants.MinOffset)
            .WithMessage(GridErrors.Validation.OffsetOutOfRange);

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);

        RuleFor(request => request.Sort!.Column)
            .Must(sortable.Contains)
            .When(request => request.Sort is not null)
            .WithMessage(GridErrors.Validation.SortColumnUnknown);

        RuleFor(request => request.Filter)
            .NotNull()
            .WithMessage(GridErrors.Validation.FilterRequired);
    }
}
