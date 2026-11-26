using System.Data;
using FluentValidation;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Application.Shifts.ChangeChecks;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Application.Shifts.Scopes;
using HealthyFac.Core.Domain.Shifts.DaySchedules;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record DeleteShiftOverrideRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    /// <summary>The local date of the company.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>The row version that the client read. The save fails when the override changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class DeleteShiftOverrideRequestValidator : AbstractValidator<DeleteShiftOverrideRequest>
{
    public DeleteShiftOverrideRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class DeleteShiftOverridePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteShiftOverrideRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureEmployeeInCompanyAsync(request.CompanyId, request.EmployeeId, cancellationToken);
    }
}

/// <summary>
/// Deletes the override of a date, so the date returns to the template. A delete of a later date removes the override
/// with its blocks in one save: the cascade removes the loaded blocks, and the save interceptor turns each removal
/// into a soft delete. A delete of today keeps the past part of the override and returns the rest to the template
/// (<see cref="TodayOverrideMerge"/>); the override row stays when the result differs from the template. The change
/// runs in one serializable transaction with the same guard as the other shift changes.
/// </summary>
/// <param name="dbContext">Saves the change.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class DeleteShiftOverrideRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard,
    TimeProvider timeProvider,
    IRealtimeNotifier notifier
) : RequestHandler<DeleteShiftOverrideRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        DeleteShiftOverrideRequest request,
        CancellationToken cancellationToken
    )
    {
        var change = await _dbContext.RunInTransactionAsync(
            token => DeleteAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);

        _notifier.PushShiftChanged(request.CompanyId, change);
    }

    private async Task<ShiftChange> DeleteAsync(DeleteShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);

        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            request.Date,
            request.Date,
            isTracked: true,
            cancellationToken);

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        ShiftDateRules.EnsureNotPast(request.Date, shifts.LocalDateOf(nowUtc));

        var existing = shifts.Overrides.SingleOrDefault()
            ?? throw new NotFoundException(ShiftErrors.Rejections.OverrideNotFound);

        var before = shifts.Resolve(request.Date, request.Date);
        _dbContext.Entry(existing).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        shifts.Overrides.Remove(existing);
        var template = shifts.Resolve(request.Date, request.Date);

        var keptBlocks = shifts.LocalDateOf(nowUtc) == request.Date
            ? TodayOverrideMerge.Merge(
                [.. before.Single().Blocks],
                [.. template.Single().Blocks],
                nowUtc,
                SlotRules.RoundUpToGrid(nowUtc))
            : null;

        IReadOnlyList<DaySchedule> after;

        if (keptBlocks is null || IsSameInstants(keptBlocks, template.Single().Blocks))
        {
            _dbContext.ShiftOverrides.Remove(existing);
            after = template;
        }
        else
        {
            existing.ReplaceBlocks(keptBlocks.Select(block => ToLocalTimes(block, zone)));
            _dbContext.ShiftOverrideBlocks.AddRange(existing.Blocks);
            _dbContext.Entry(existing).State = EntityState.Modified;
            shifts.Overrides.Add(existing);
            after = shifts.Resolve(request.Date, request.Date);
        }

        await _guard.EnsureAsync(request.CompanyId, request.EmployeeId, zone, before, after, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
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

    private static bool IsSameInstants(IReadOnlyList<DayBlock> first, IReadOnlyList<DayBlock> second) =>
        first.Select(block => (block.StartUtc, block.EndUtc))
            .SequenceEqual(second.Select(block => (block.StartUtc, block.EndUtc)));

    private static BlockTimes ToLocalTimes(DayBlock block, TimeZoneInfo zone) =>
        new()
        {
            Start = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(block.StartUtc, zone)),
            End = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(block.EndUtc, zone)),
        };
}
