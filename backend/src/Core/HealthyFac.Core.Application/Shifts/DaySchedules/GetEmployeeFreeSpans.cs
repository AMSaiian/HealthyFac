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

public record GetEmployeeFreeSpansRequest : IRequest<FreeSpansResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required DateOnly Date { get; init; }

    /// <summary>A work order whose slot counts as free, or <see langword="null"/>.</summary>
    public Guid? ExcludeWorkOrderId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }
}

/// <summary>
/// The free spans of one date. The bounds are on the 5-minute grid, in local times and as UTC instants.
/// </summary>
public record FreeSpansResponse
{
    public required DateOnly Date { get; init; }

    /// <summary>The IANA zone of the company. The local times of the response are times in this zone.</summary>
    public required string TimeZoneId { get; init; }

    public required IReadOnlyList<FreeSpanResponse> Spans { get; init; }
}

public record FreeSpanResponse
{
    public required DateTime StartLocal { get; init; }

    public required DateTime EndLocal { get; init; }

    public required DateTime StartUtc { get; init; }

    public required DateTime EndUtc { get; init; }
}

public class GetEmployeeFreeSpansRequestValidator : AbstractValidator<GetEmployeeFreeSpansRequest>
{
    public GetEmployeeFreeSpansRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class GetEmployeeFreeSpansPreHandlerCheck(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<GetEmployeeFreeSpansRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetEmployeeFreeSpansRequest request, CancellationToken cancellationToken)
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
/// Reads the free spans of one employee on one date, from now on the grid for today.
/// </summary>
/// <param name="reader">Loads the shifts.</param>
/// <param name="occupancyReader">Reads the occupied time.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class GetEmployeeFreeSpansRequestHandler(
    DayScheduleReader reader,
    EmployeeOccupancyReader occupancyReader,
    TimeProvider timeProvider
) : IRequestHandler<GetEmployeeFreeSpansRequest, FreeSpansResponse>
{
    private readonly DayScheduleReader _reader = reader;
    private readonly EmployeeOccupancyReader _occupancyReader = occupancyReader;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<FreeSpansResponse> HandleAsync(
        GetEmployeeFreeSpansRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            request.Date,
            request.Date,
            isTracked: false,
            cancellationToken);
        var occupancies = await _occupancyReader.ReadAsync(
            request.CompanyId,
            request.EmployeeId,
            LocalTimes.ToUtc(request.Date.ToDateTime(TimeOnly.MinValue), zone),
            LocalTimes.ToUtc(request.Date.AddDays(value: 1).ToDateTime(TimeOnly.MinValue), zone),
            request.ExcludeWorkOrderId,
            cancellationToken);

        var spans = FreeSpans.Of(
                shifts.Resolve(request.Date, request.Date).Single(),
                occupancies,
                _timeProvider.GetUtcNow().UtcDateTime,
                zone)
            .Select(span => new FreeSpanResponse
            {
                StartLocal = span.StartLocal,
                EndLocal = span.EndLocal,
                StartUtc = span.StartUtc,
                EndUtc = span.EndUtc,
            })
            .ToList();

        return new FreeSpansResponse { Date = request.Date, TimeZoneId = zone.Id, Spans = spans };
    }
}
