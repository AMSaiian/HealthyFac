using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.Time;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.ResendEmployeeInvite;

public record ResendEmployeeInviteRequest : IRequest<ResendEmployeeInviteResponse>
{
    public required Guid CompanyId { get; init; }
    public required Guid InviteId { get; init; }
}

public record ResendEmployeeInviteResponse
{
    public required DateTime ExpiresAt { get; init; }
}

public class ResendEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IOutbox outbox,
    IDateProvider dateProvider
) : IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>
{
    public async Task<ResendEmployeeInviteResponse> HandleAsync(
        ResendEmployeeInviteRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var invite = await dbContext.EmployeeInvites.SingleOrDefaultAsync(
                invite => invite.Id == request.InviteId && invite.CompanyId == request.CompanyId,
                ct)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteNotFound);

        if (invite.AcceptedAt is not null)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.InviteAlreadyAccepted);
        }

        invite.Renew(tokenService.CreateSalt(), tokenService.GetExpiresAt(dateProvider.GetUtc()));
        outbox.Add(
            new EmployeeInvited
            {
                InviteId = invite.Id,
                CompanyId = invite.CompanyId,
                Email = invite.Email,
            });
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.InviteChanged, exception);
        }

        return new ResendEmployeeInviteResponse { ExpiresAt = invite.ExpiresAt };
    }
}
