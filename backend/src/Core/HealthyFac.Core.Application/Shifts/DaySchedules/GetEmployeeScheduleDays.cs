using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts.Scopes;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Time;

namespace HealthyFac.Core.Application.Shifts.DaySchedules;

public record GetEmployeeScheduleDaysRequest : IRequest<ScheduleDaysResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required DateOnly From { get; init; }

    public required DateOnly To { get; init; }

    /// <summary>A work order whose slot counts as free, or <see langword="null"/>.</summary>
    public Guid? ExcludeWorkOrderId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

/// <summary>
/// The summary of each date of a range: whether the day has work blocks, and the longest free span in minutes.
/// </summary>
public record ScheduleDaysResponse
{
    /// <summary>The IANA zone of the company.</summary>
    public required string TimeZoneId { get; init; }

    public required IReadOnlyList<ScheduleDayResponse> Days { get; init; }
}

public record ScheduleDayResponse
{
    public required DateOnly Date { get; init; }

    public required bool HasBlocks { get; init; }

    /// <summary>The longest free span of the day in real minutes, from now on the grid for today.</summary>
    public required int LongestFreeMinutes { get; init; }
}

public class GetEmployeeScheduleDaysRequestValidator : AbstractValidator<GetEmployeeScheduleDaysRequest>
{
    public GetEmployeeScheduleDaysRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.To)
            .GreaterThanOrEqualTo(request => request.From)
            .WithMessage(ShiftErrors.Validation.RangeInvalid);
    }
}

public sealed class GetEmployeeScheduleDaysPreHandlerCheck(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<GetEmployeeScheduleDaysRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetEmployeeScheduleDaysRequest request, CancellationToken cancellationToken)
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
/// Summarizes each date of a range for one employee: whether the day has blocks, and its longest free span. A past
/// date has no free span, and today counts only from now on the grid.
/// </summary>
/// <param name="reader">Loads the shifts.</param>
/// <param name="occupancyReader">Reads the occupied time.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class GetEmployeeScheduleDaysRequestHandler(
    DayScheduleReader reader,
    EmployeeOccupancyReader occupancyReader,
    TimeProvider timeProvider
) : IRequestHandler<GetEmployeeScheduleDaysRequest, ScheduleDaysResponse>
{
    private readonly DayScheduleReader _reader = reader;
    private readonly EmployeeOccupancyReader _occupancyReader = occupancyReader;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<ScheduleDaysResponse> HandleAsync(
        GetEmployeeScheduleDaysRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            request.From,
            request.To,
            isTracked: false,
            cancellationToken);
        var occupancies = await _occupancyReader.ReadAsync(
            request.CompanyId,
            request.EmployeeId,
            LocalTimes.ToUtc(request.From.ToDateTime(TimeOnly.MinValue), zone),
            LocalTimes.ToUtc(request.To.AddDays(value: 1).ToDateTime(TimeOnly.MinValue), zone),
            request.ExcludeWorkOrderId,
            cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var days = shifts.Resolve(request.From, request.To)
            .Select(day => new ScheduleDayResponse
            {
                Date = day.Date,
                HasBlocks = day.Blocks.Count > 0,
                LongestFreeMinutes = FreeSpans.LongestMinutes(FreeSpans.Of(day, occupancies, nowUtc, zone)),
            })
            .ToList();

        return new ScheduleDaysResponse { TimeZoneId = zone.Id, Days = days };
    }
}
