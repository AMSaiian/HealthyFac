using FluentValidation;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record GetCompanyDayScheduleRequest : IRequest<CompanyDayScheduleResponse>
{
    public required Guid CompanyId { get; init; }

    public required DateOnly Date { get; init; }
}

public record CompanyDayScheduleResponse
{
    public required DateOnly Date { get; init; }

    public required string TimeZoneId { get; init; }

    public required IReadOnlyList<CompanyDayScheduleEmployeeResponse> Employees { get; init; }
}

public record CompanyDayScheduleEmployeeResponse
{
    public required Guid EmployeeId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required DayScheduleResponse Day { get; init; }
}

public class GetCompanyDayScheduleRequestValidator : AbstractValidator<GetCompanyDayScheduleRequest>
{
    public GetCompanyDayScheduleRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);
    }
}

public sealed class GetCompanyDaySchedulePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetCompanyDayScheduleRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetCompanyDayScheduleRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the day of every active employee of the company, ordered by name. The endpoint needs the manage permission,
/// so the use case has no scope.
/// </summary>
/// <param name="dbContext">Reads the active employees.</param>
/// <param name="builder">Builds the days.</param>
/// <param name="reader">Reads the zone of the company.</param>
public sealed class GetCompanyDayScheduleRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleBuilder builder,
    DayScheduleReader reader
) : IRequestHandler<GetCompanyDayScheduleRequest, CompanyDayScheduleResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleBuilder _builder = builder;
    private readonly DayScheduleReader _reader = reader;

    public async Task<CompanyDayScheduleResponse> HandleAsync(
        GetCompanyDayScheduleRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.CompanyId == request.CompanyId && employee.Status == EmployeeStatus.Active)
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.LastName)
            .ThenBy(employee => employee.Id)
            .Select(employee => new { employee.Id, employee.FirstName, employee.LastName })
            .ToListAsync(cancellationToken);
        var days = await _builder.BuildAsync(
            request.CompanyId,
            employees.Select(employee => employee.Id).ToList(),
            request.Date,
            cancellationToken);
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);

        return new CompanyDayScheduleResponse
        {
            Date = request.Date,
            TimeZoneId = zone.Id,
            Employees = employees
                .Select(employee => new CompanyDayScheduleEmployeeResponse
                {
                    EmployeeId = employee.Id,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Day = days[employee.Id],
                })
                .ToList(),
        };
    }
}
