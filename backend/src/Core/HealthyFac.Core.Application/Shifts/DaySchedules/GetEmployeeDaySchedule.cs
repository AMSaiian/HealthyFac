using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Core.Application.Shifts;

public record GetEmployeeDayScheduleRequest : IRequest<DayScheduleResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required DateOnly Date { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

public class GetEmployeeDayScheduleRequestValidator : AbstractValidator<GetEmployeeDayScheduleRequest>
{
    public GetEmployeeDayScheduleRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class GetEmployeeDaySchedulePreHandlerCheck(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<GetEmployeeDayScheduleRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetEmployeeDayScheduleRequest request, CancellationToken cancellationToken)
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
/// Reads the day of one employee: the blocks and the scheduled work orders that start on the date.
/// </summary>
/// <param name="builder">Builds the day.</param>
public sealed class GetEmployeeDayScheduleRequestHandler(DayScheduleBuilder builder)
    : IRequestHandler<GetEmployeeDayScheduleRequest, DayScheduleResponse>
{
    private readonly DayScheduleBuilder _builder = builder;

    public async Task<DayScheduleResponse> HandleAsync(
        GetEmployeeDayScheduleRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var days = await _builder.BuildAsync(request.CompanyId, [request.EmployeeId], request.Date, cancellationToken);

        return days[request.EmployeeId];
    }
}
