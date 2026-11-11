using System.Data;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
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
/// Deletes the override of a date with its blocks in one save, so the date returns to the template. The change runs in
/// one serializable transaction with the same guard as the other shift changes.
/// </summary>
/// <param name="dbContext">Saves the change.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
public sealed class DeleteShiftOverrideRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard
) : RequestHandler<DeleteShiftOverrideRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;

    protected override async Task HandleInternalAsync(
        DeleteShiftOverrideRequest request,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.RunInTransactionAsync(
            token => DeleteAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task DeleteAsync(DeleteShiftOverrideRequest request, CancellationToken cancellationToken)
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            request.Date,
            request.Date,
            isTracked: true,
            cancellationToken);
        var existing = shifts.Overrides.SingleOrDefault()
            ?? throw new NotFoundException(ShiftErrors.Rejections.OverrideNotFound);

        var before = shifts.Resolve(request.Date, request.Date);
        _dbContext.Entry(existing).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        _dbContext.ShiftOverrideBlocks.RemoveRange(existing.Blocks);
        _dbContext.ShiftOverrides.Remove(existing);
        shifts.Overrides.Remove(existing);
        var after = shifts.Resolve(request.Date, request.Date);

        await _guard.EnsureAsync(request.CompanyId, request.EmployeeId, zone, before, after, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(ShiftErrors.Rejections.OverrideChanged, exception);
        }
    }
}
