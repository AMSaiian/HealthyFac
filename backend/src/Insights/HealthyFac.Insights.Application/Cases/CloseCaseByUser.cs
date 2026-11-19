using FluentValidation;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Cases;

public record CloseCaseByUserRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required string Reason { get; init; }
}

public sealed class CloseCaseByUserRequestValidator : AbstractValidator<CloseCaseByUserRequest>
{
    public CloseCaseByUserRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(CaseErrors.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(CaseErrors.CaseIdRequired);
        RuleFor(request => request.Reason)
            .NotEmpty()
            .SingleLineText()
            .MaximumLength(CaseLimits.CloseReasonMaxLength);
    }
}

public sealed class CloseCaseByUserPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CloseCaseByUserRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CloseCaseByUserRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Closes an open case for a user, with the reason of the user. Close dismisses the uncovered signals. A case that
/// is not open is an error.
/// </summary>
/// <param name="dbContext">Loads and saves the case.</param>
/// <param name="timeProvider">Provides the close time.</param>
public sealed class CloseCaseByUserRequestHandler(IInsightsDbContext dbContext, TimeProvider timeProvider)
    : RequestHandler<CloseCaseByUserRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        CloseCaseByUserRequest request,
        CancellationToken cancellationToken
    )
    {
        var maintenanceCase = await _dbContext.MaintenanceCases
                .Include(candidate => candidate.Signals)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(CaseErrors.CaseNotFound);

        if (maintenanceCase.State != CaseState.Open)
        {
            throw new UnprocessableException(CaseErrors.CaseNotOpen);
        }

        maintenanceCase.Close(request.Reason, _timeProvider.GetUtcNow().UtcDateTime);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
