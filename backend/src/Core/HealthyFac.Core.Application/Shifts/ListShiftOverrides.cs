using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record ListShiftOverridesRequest : IRequest<IReadOnlyList<ShiftOverrideResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public record ShiftOverrideResponse
{
    public required Guid Id { get; init; }

    public required DateOnly Date { get; init; }

    public required IReadOnlyList<ShiftBlockInput> Blocks { get; init; }

    public required uint RowVersion { get; init; }
}

public class ListShiftOverridesRequestValidator : AbstractValidator<ListShiftOverridesRequest>
{
    public ListShiftOverridesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .WithMessage(ShiftErrors.Validation.RangeInvalid);

        RuleFor(request => request)
            .Must(request =>
                request.To.DayNumber - request.From.DayNumber < ShiftValidationConstants.MaxOverrideRangeDays)
            .When(request => request.To >= request.From)
            .WithMessage(ShiftErrors.Validation.RangeTooLong);
    }
}

public sealed class ListShiftOverridesPreHandlerCheck(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<ListShiftOverridesRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ListShiftOverridesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await request.Scope.EnsureEmployeeReadableAsync(
            _dbContext,
            _ownEmployee,
            request.CompanyId,
            request.EmployeeId,
            cancellationToken);
    }
}

/// <summary>
/// Lists the overrides of one employee in a date range of at most 62 days, by date.
/// </summary>
/// <param name="dbContext">Reads the overrides.</param>
public sealed class ListShiftOverridesRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<ListShiftOverridesRequest, IReadOnlyList<ShiftOverrideResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<ShiftOverrideResponse>> HandleAsync(
        ListShiftOverridesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var rows = await _dbContext.ShiftOverrides
            .AsNoTracking()
            .Include(shiftOverride => shiftOverride.Blocks)
            .Where(shiftOverride => shiftOverride.EmployeeId == request.EmployeeId
                && shiftOverride.Date >= request.From
                && shiftOverride.Date <= request.To)
            .OrderBy(shiftOverride => shiftOverride.Date)
            .Select(shiftOverride => new
            {
                Override = shiftOverride,
                shiftOverride.RowVersion,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ShiftOverrideResponse
            {
                Id = row.Override.Id,
                Date = row.Override.Date,
                RowVersion = row.RowVersion,
                Blocks = row.Override.Blocks
                    .OrderBy(block => block.Start)
                    .Select(block => new ShiftBlockInput { Start = block.Start, End = block.End })
                    .ToList(),
            })
            .ToList();
    }
}
