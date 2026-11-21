using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Application.Shifts.BlockInput;
using HealthyFac.Core.Application.Shifts.ChangeChecks;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Application.Shifts.Scopes;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record SetShiftOverrideRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    /// <summary>The local date of the company.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The blocks of the day. An empty list is a day off.</summary>
    public required IReadOnlyList<ShiftBlockInput> Blocks { get; init; }

    /// <summary>
    /// The row version that the client read, or <see langword="null"/> when the client creates the override. The save
    /// fails when the override changed since.
    /// </summary>
    public uint? RowVersion { get; init; }
}

public class SetShiftOverrideRequestValidator : AbstractValidator<SetShiftOverrideRequest>
{
    public SetShiftOverrideRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.Blocks)
            .NotNull()
            .WithMessage(ShiftErrors.Validation.BlocksRequired)
            .SetValidator(new BlockListValidator());
    }
}

public sealed class SetShiftOverridePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SetShiftOverrideRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(SetShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureEmployeeInCompanyAsync(request.CompanyId, request.EmployeeId, cancellationToken);
    }
}

/// <summary>
/// Sets the blocks of one date, from today up to 365 days ahead. A new date creates the override. An existing date
/// replaces its blocks and needs the row version of the client: the handler marks the override row modified, so the
/// save checks and changes <c>xmin</c>. A parallel create of the same date and a stale row version both end in the
/// conflict (409). The change runs in one serializable transaction with the same guard as the template versions.
/// </summary>
/// <param name="dbContext">Saves the change.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class SetShiftOverrideRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard,
    TimeProvider timeProvider,
    IRealtimeNotifier notifier
) : RequestHandler<SetShiftOverrideRequest>
{
    private const string DateKey = "date";

    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        SetShiftOverrideRequest request,
        CancellationToken cancellationToken
    )
    {
        var change = await _dbContext.RunInTransactionAsync(
            token => SetAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
        _notifier.PushShiftChanged(request.CompanyId, change);
    }

    private async Task<ShiftChange> SetAsync(SetShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            request.Date,
            request.Date,
            isTracked: true,
            cancellationToken);
        var today = shifts.LocalDateOf(_timeProvider.GetUtcNow().UtcDateTime);
        if (request.Date < today || request.Date > today.AddDays(ShiftValidationConstants.MaxDaysAhead))
        {
            throw new AppValidationException(
                [new ValidationFailure(DateKey, ShiftErrors.Validation.DateOutOfRange)]);
        }

        var before = shifts.Resolve(request.Date, request.Date);
        var times = request.Blocks.Select(block => block.ToTimes()).ToList();
        var existing = shifts.Overrides.SingleOrDefault();
        if (existing is null)
        {
            ReplaceByCreate(request, shifts, times);
        }
        else
        {
            ReplaceBlocks(request, existing, times);
        }

        var after = shifts.Resolve(request.Date, request.Date);

        await _guard.EnsureAsync(request.CompanyId, request.EmployeeId, zone, before, after, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is UniqueConstraintException or DbUpdateConcurrencyException)
        {
            throw new ConflictException(ShiftErrors.Rejections.OverrideChanged, exception);
        }

        var identityUserId = await _dbContext.FindIdentityUserIdAsync(request.EmployeeId, cancellationToken);

        return new ShiftChange
        {
            EmployeeId = request.EmployeeId,
            IdentityUserId = identityUserId,
            Date = request.Date,
        };
    }

    private void ReplaceByCreate(SetShiftOverrideRequest request, EmployeeShifts shifts, List<BlockTimes> times)
    {
        if (request.RowVersion is not null)
        {
            throw new ConflictException(ShiftErrors.Rejections.OverrideChanged);
        }

        var shiftOverride = new ShiftOverride(request.EmployeeId, request.Date, times);
        _dbContext.ShiftOverrides.Add(shiftOverride);
        shifts.Overrides.Add(shiftOverride);
    }

    private void ReplaceBlocks(SetShiftOverrideRequest request, ShiftOverride existing, List<BlockTimes> times)
    {
        if (request.RowVersion is not { } rowVersion)
        {
            throw new ConflictException(ShiftErrors.Rejections.OverrideChanged);
        }

        _dbContext.Entry(existing).Property(stored => stored.RowVersion).OriginalValue = rowVersion;
        existing.ReplaceBlocks(times);
        _dbContext.ShiftOverrideBlocks.AddRange(existing.Blocks);
        _dbContext.Entry(existing).State = EntityState.Modified;
    }
}
