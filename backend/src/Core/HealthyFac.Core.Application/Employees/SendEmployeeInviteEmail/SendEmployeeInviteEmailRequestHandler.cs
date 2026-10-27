using HealthyFac.Core.Application.Employees.InviteLinks;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Application.Links;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;

/// <summary>
/// Emails the invited person a link with a fresh token. The handler builds the token from the current salt of the
/// invite, so a resend before the first email leaves no dead link. The token never enters the database, the outbox,
/// a log message, or an exception message.
/// </summary>
/// <remarks>
/// Sending the email is the only durable step, and it is the last step. A stop or a crash after the send and
/// before the inbox commit sends the email again on the next attempt. Both links work, because each one is built
/// from the same current salt. The handler skips an invite that is missing, accepted, or expired and completes, so
/// the message does not retry forever. A sender failure propagates, so the inbox schedules a retry.
/// </remarks>
/// <param name="dbContext">Reads the invite.</param>
/// <param name="tokenService">Creates the link token.</param>
/// <param name="emailSender">Sends the email.</param>
/// <param name="emailBuilder">Builds the email from the link and the expiry time.</param>
/// <param name="linkOptions">Holds the base URL of the web app.</param>
/// <param name="timeProvider">Provides the current time.</param>
/// <param name="logger">Logs skipped invites.</param>
public sealed class SendEmployeeInviteEmailRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IEmailSender emailSender,
    IEmployeeInviteEmailBuilder emailBuilder,
    IOptions<InviteLinkOptions> linkOptions,
    TimeProvider timeProvider,
    ILogger<SendEmployeeInviteEmailRequestHandler> logger
) : RequestHandler<EmployeeInvited>
{
    protected override async Task HandleInternalAsync(EmployeeInvited request, CancellationToken ct)
    {
        var invite = await dbContext.EmployeeInvites
            .AsNoTracking()
            .SingleOrDefaultAsync(invite => invite.Id == request.InviteId, ct);

        if (invite is null)
        {
            logger.LogInformation("Invite email skipped: invite {InviteId} does not exist.", request.InviteId);

            return;
        }

        if (!invite.CanAccept(timeProvider.GetUtcNow().UtcDateTime))
        {
            logger.LogInformation(
                "Invite email skipped: invite {InviteId} is accepted or expired. A supervisor must resend it.",
                invite.Id);

            return;
        }

        var model = new EmployeeInviteEmailModel
        {
            To = invite.Email,
            Link = BuildLink(tokenService.CreateToken(invite.Id, invite.TokenSalt)),
            ExpiresAt = invite.ExpiresAt,
        };
        var email = await emailBuilder.BuildAsync(model, ct);

        await emailSender.SendAsync(email, ct);
    }

    private string BuildLink(string token) =>
        $"{linkOptions.Value.SpaBaseUrl.TrimEnd(LinkConstants.PathSeparator)}"
        + $"{InviteLinkConstants.InvitePath}#{InviteLinkConstants.TokenFragmentKey}={token}";
}
