using FluentValidation;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record ResendEmployeeInviteRequest : IRequest<ResendEmployeeInviteResponse>
{
    public required Guid CompanyId { get; init; }
    public required Guid InviteId { get; init; }
}

public record ResendEmployeeInviteResponse
{
    public required DateTime ExpiresAt { get; init; }
}

public class ResendEmployeeInviteRequestValidator : AbstractValidator<ResendEmployeeInviteRequest>
{
    public ResendEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(x => x.InviteId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.InviteIdRequired);
    }
}

public sealed class ResendEmployeeInvitePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ResendEmployeeInviteRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ResendEmployeeInviteRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

public class ResendEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IInviteTokenService _tokenService = tokenService;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<ResendEmployeeInviteResponse> HandleAsync(
        ResendEmployeeInviteRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var invite = await _dbContext.EmployeeInvites.SingleOrDefaultAsync(
                invite => invite.Id == request.InviteId && invite.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteNotFound);

        if (invite.AcceptedAt is not null)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.InviteAlreadyAccepted);
        }

        invite.Renew(_tokenService.CreateSalt(), _tokenService.GetExpiresAt(_timeProvider.GetUtcNow().UtcDateTime));
        _outbox.Add(
            new EmployeeInvited
            {
                InviteId = invite.Id,
                CompanyId = invite.CompanyId,
                Email = invite.Email,
            });
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.InviteChanged, exception);
        }

        return new ResendEmployeeInviteResponse { ExpiresAt = invite.ExpiresAt };
    }
}
