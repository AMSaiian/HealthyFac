using System.Data;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record DeleteShiftTemplateVersionRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Guid VersionId { get; init; }

    /// <summary>The row version that the client read. The save fails when the version changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class DeleteShiftTemplateVersionRequestValidator : AbstractValidator<DeleteShiftTemplateVersionRequest>
{
    public DeleteShiftTemplateVersionRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.VersionId).NotEmpty().WithMessage(ShiftErrors.Validation.VersionIdRequired);
    }
}

public sealed class DeleteShiftTemplateVersionPreHandlerCheck(
    ICoreDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<DeleteShiftTemplateVersionRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteShiftTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureEmployeeInCompanyAsync(request.CompanyId, request.EmployeeId, cancellationToken);
    }
}

/// <summary>
/// Deletes a version that has not taken effect yet, with its blocks, in one save inside a serializable transaction.
/// The guard checks the day schedules without the version, like the update does.
/// </summary>
/// <param name="dbContext">Saves the change.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class DeleteShiftTemplateVersionRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard,
    TimeProvider timeProvider
) : RequestHandler<DeleteShiftTemplateVersionRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        DeleteShiftTemplateVersionRequest request,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.RunInTransactionAsync(
            token => DeleteAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task DeleteAsync(DeleteShiftTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var shifts = await _reader.LoadAsync(
            request.EmployeeId,
            zone,
            DateOnly.FromDateTime(nowUtc).AddDays(value: -2),
            DateOnly.FromDateTime(nowUtc).AddDays(ShiftValidationConstants.MaxDaysAhead + 2),
            isTracked: true,
            cancellationToken);
        var version = shifts.Versions.SingleOrDefault(candidate => candidate.Id == request.VersionId);
        if (version is null)
        {
            throw await ShiftVersionRejections.MissingAsync(_dbContext, request.VersionId, cancellationToken);
        }

        var today = shifts.LocalDateOf(nowUtc);
        if (version.EffectiveFrom <= today)
        {
            throw new UnprocessableException(ShiftErrors.Rejections.VersionInEffect);
        }

        var lastDate = today.AddDays(ShiftValidationConstants.MaxDaysAhead);
        var before = shifts.Resolve(version.EffectiveFrom, lastDate);

        _dbContext.Entry(version).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        _dbContext.ShiftTemplateBlocks.RemoveRange(version.Blocks);
        _dbContext.ShiftTemplateVersions.Remove(version);
        shifts.Versions.Remove(version);
        var after = shifts.Resolve(version.EffectiveFrom, lastDate);

        await _guard.EnsureAsync(request.CompanyId, request.EmployeeId, zone, before, after, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(ShiftErrors.Rejections.VersionChanged, exception);
        }
    }
}
