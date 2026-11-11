using System.Data;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Shifts;

public record UpdateShiftTemplateVersionRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    public required Guid VersionId { get; init; }

    public required IReadOnlyList<ShiftTemplateDayInput> Days { get; init; }

    /// <summary>The row version that the client read. The save fails when the version changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class UpdateShiftTemplateVersionRequestValidator : AbstractValidator<UpdateShiftTemplateVersionRequest>
{
    public UpdateShiftTemplateVersionRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.VersionId).NotEmpty().WithMessage(ShiftErrors.Validation.VersionIdRequired);

        RuleFor(request => request.Days).SetValidator(new ShiftTemplateDaysValidator());
    }
}

public sealed class UpdateShiftTemplateVersionPreHandlerCheck(
    ICoreDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<UpdateShiftTemplateVersionRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UpdateShiftTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureEmployeeInCompanyAsync(request.CompanyId, request.EmployeeId, cancellationToken);
    }
}

/// <summary>
/// Replaces the blocks of a version that has not taken effect yet. The handler sets the original row version from the
/// request and marks the version row modified, so the save checks and changes <c>xmin</c>: a parallel change ends in
/// the conflict (409). The change runs in one serializable transaction with the same guard as the create.
/// </summary>
/// <param name="dbContext">Saves the change.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class UpdateShiftTemplateVersionRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard,
    TimeProvider timeProvider
) : RequestHandler<UpdateShiftTemplateVersionRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        UpdateShiftTemplateVersionRequest request,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.RunInTransactionAsync(
            token => UpdateAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task UpdateAsync(UpdateShiftTemplateVersionRequest request, CancellationToken cancellationToken)
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
        var removedBlocks = version.ReplaceBlocks(ShiftTemplateDayInput.ToTemplateBlocks(request.Days));
        _dbContext.ShiftTemplateBlocks.RemoveRange(removedBlocks);
        _dbContext.ShiftTemplateBlocks.AddRange(version.Blocks);
        _dbContext.Entry(version).State = EntityState.Modified;
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
