using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record ListShiftTemplateVersionsRequest : IRequest<IReadOnlyList<ShiftTemplateVersionResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public record ShiftTemplateVersionResponse
{
    public required Guid Id { get; init; }

    public required DateOnly EffectiveFrom { get; init; }

    /// <summary>One entry for each weekday, Monday first.</summary>
    public required IReadOnlyList<ShiftTemplateDayResponse> Days { get; init; }

    public required uint RowVersion { get; init; }
}

public record ShiftTemplateDayResponse
{
    public required Weekday Weekday { get; init; }

    public required IReadOnlyList<ShiftBlockInput> Blocks { get; init; }
}

public class ListShiftTemplateVersionsRequestValidator : AbstractValidator<ListShiftTemplateVersionsRequest>
{
    public ListShiftTemplateVersionsRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class ListShiftTemplateVersionsPreHandlerCheck(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<ListShiftTemplateVersionsRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ListShiftTemplateVersionsRequest request, CancellationToken cancellationToken)
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
/// Lists the template versions of one employee, the oldest first, each with its seven days.
/// </summary>
/// <param name="dbContext">Reads the versions.</param>
public sealed class ListShiftTemplateVersionsRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<ListShiftTemplateVersionsRequest, IReadOnlyList<ShiftTemplateVersionResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<ShiftTemplateVersionResponse>> HandleAsync(
        ListShiftTemplateVersionsRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var rows = await _dbContext.ShiftTemplateVersions
            .AsNoTracking()
            .Include(version => version.Blocks)
            .Where(version => version.EmployeeId == request.EmployeeId)
            .OrderBy(version => version.EffectiveFrom)
            .Select(version => new
            {
                Version = version,
                version.RowVersion,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ShiftTemplateVersionResponse
            {
                Id = row.Version.Id,
                EffectiveFrom = row.Version.EffectiveFrom,
                RowVersion = row.RowVersion,
                Days = Weekday.All
                    .Select(weekday => new ShiftTemplateDayResponse
                    {
                        Weekday = weekday,
                        Blocks = row.Version.Blocks
                            .Where(block => block.Weekday == weekday)
                            .OrderBy(block => block.Start)
                            .Select(block => new ShiftBlockInput { Start = block.Start, End = block.End })
                            .ToList(),
                    })
                    .ToList(),
            })
            .ToList();
    }
}
