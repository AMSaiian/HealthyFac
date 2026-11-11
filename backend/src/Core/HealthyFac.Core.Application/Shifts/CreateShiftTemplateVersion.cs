using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Core.Application.Shifts;

public record CreateShiftTemplateVersionRequest : IRequest<CreateShiftTemplateVersionResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }

    /// <summary>The first local date of the company on which the version applies.</summary>
    public required DateOnly EffectiveFrom { get; init; }

    public required IReadOnlyList<ShiftTemplateDayInput> Days { get; init; }
}

public record CreateShiftTemplateVersionResponse
{
    public required Guid Id { get; init; }
}

public class CreateShiftTemplateVersionRequestValidator : AbstractValidator<CreateShiftTemplateVersionRequest>
{
    public CreateShiftTemplateVersionRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(ShiftErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId).NotEmpty().WithMessage(ShiftErrors.Validation.EmployeeIdRequired);

        RuleFor(request => request.Days).SetValidator(new ShiftTemplateDaysValidator());
    }
}

public sealed class CreateShiftTemplateVersionPreHandlerCheck(
    ICoreDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<CreateShiftTemplateVersionRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(CreateShiftTemplateVersionRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);
        await _dbContext.EnsureEmployeeInCompanyAsync(request.CompanyId, request.EmployeeId, cancellationToken);
    }
}

/// <summary>
/// Creates a template version that takes effect on a date from today up to 365 days ahead. The whole change runs in one
/// serializable transaction: the handler resolves the day schedules before and after, lets the guard reject a change
/// that breaks the past part of today or orphans a scheduled work order, and saves.
/// </summary>
/// <remarks>
/// The operation reads everything again on each attempt, so a serialization failure that the retry strategy repeats
/// is safe. Every call observes the token: a cancel rolls the transaction back.
/// </remarks>
/// <param name="dbContext">Saves the version.</param>
/// <param name="reader">Loads the shift data.</param>
/// <param name="guard">Rejects a change that breaks a rule.</param>
/// <param name="timeProvider">Provides the current time.</param>
public sealed class CreateShiftTemplateVersionRequestHandler(
    ICoreDbContext dbContext,
    DayScheduleReader reader,
    ShiftChangeGuard guard,
    TimeProvider timeProvider
) : IRequestHandler<CreateShiftTemplateVersionRequest, CreateShiftTemplateVersionResponse>
{
    private const string EffectiveFromKey = "effectiveFrom";

    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly DayScheduleReader _reader = reader;
    private readonly ShiftChangeGuard _guard = guard;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<CreateShiftTemplateVersionResponse> HandleAsync(
        CreateShiftTemplateVersionRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var versionId = await _dbContext.RunInTransactionAsync(
            token => CreateAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);

        return new CreateShiftTemplateVersionResponse { Id = versionId };
    }

    private async Task<Guid> CreateAsync(CreateShiftTemplateVersionRequest request, CancellationToken cancellationToken)
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
        var today = shifts.LocalDateOf(nowUtc);
        var lastDate = today.AddDays(ShiftValidationConstants.MaxDaysAhead);
        if (request.EffectiveFrom < today || request.EffectiveFrom > lastDate)
        {
            throw new AppValidationException(
                [new ValidationFailure(EffectiveFromKey, ShiftErrors.Validation.EffectiveFromOutOfRange)]);
        }

        if (shifts.Versions.Any(version => version.EffectiveFrom == request.EffectiveFrom))
        {
            throw new ConflictException(ShiftErrors.Rejections.EffectiveFromTaken);
        }

        var before = shifts.Resolve(request.EffectiveFrom, lastDate);
        var version = new ShiftTemplateVersion(
            request.EmployeeId,
            request.EffectiveFrom,
            ShiftTemplateDayInput.ToTemplateBlocks(request.Days));
        _dbContext.ShiftTemplateVersions.Add(version);
        shifts.Versions.Add(version);
        var after = shifts.Resolve(request.EffectiveFrom, lastDate);

        await _guard.EnsureAsync(request.CompanyId, request.EmployeeId, zone, before, after, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(ShiftErrors.Rejections.EffectiveFromTaken, exception);
        }

        return version.Id;
    }
}
